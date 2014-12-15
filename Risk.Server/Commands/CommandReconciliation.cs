using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using NLog;

namespace Risk.Commands
{
    /// <summary>
    /// Команда сверки
    /// </summary>
    [Command("Reconciliation")]
    public class CommandReconciliation : CommandServer
    {
        /// <summary>
        /// рефандинг для всех
        /// </summary>
        private const int RefundLevelForAll = 2;

        /// <summary>
        /// рефандинг только для refund-клиентов
        /// </summary>
        private const int RefundLevelForRefundingClients = 1;

        /// <summary>
        /// Список внешних кодов инструментов по которым не производится сверка
        /// </summary>
        private static readonly List<string> NotReconcilingExternalInstrumentCodes = new List<string>
        {
            "RUR"
        };

        /// <summary>
        /// Точка входа выполнения команды
        /// </summary>
        protected internal override void InternalExecute()
        {
            var reconciliationData = MakeReconciliation();

            SetResult(reconciliationData.ToArray());
        }

        /// <summary>
        /// Выполняет сверку
        /// </summary>
        /// <returns></returns>
        private static List<ReconciliationPosition> MakeReconciliation()
        {
            var mmaPositions = LoadMMAPositions(); // получаем данные из ММА
            var externalPositions = new List<PositionSummary>();

            var etnaPositions = LoadETNAPositions(); // получает данные из Этны
            var rrmPositions = LoadRRMPositions(); // получаем данные из RRM
            var fortsPositions = LoadFORTSPositions(); // получаем данные из FORTS

            if (etnaPositions != null)
                externalPositions.AddRange(etnaPositions);
            if (rrmPositions != null)
                externalPositions.AddRange(rrmPositions);
            if (fortsPositions != null)
                externalPositions.AddRange(fortsPositions);

            // сверка
            return MakeReconciliation(mmaPositions, externalPositions);
        }

        /// <summary>
        ///  Получает позиции по ММА
        /// </summary>
        /// <returns></returns>
        private static List<PositionSummary> LoadMMAPositions()
        {
            // джойним позиции с инструментами по коду инстурмента и портфелями по номеру счета
            // выбираем по следующему условию:
            // refund_level инструмента = 2 или (refund_level инструмента = 1 «И» для клиента включен refund)
            // Далее группируем по коду инструмента и находим сумму позиций, которую умножаем на размер лота

            var positionSummary = from position in Server.Positions
                                  join instrument in Server.Instruments on position.SecCode equals instrument.SecCode
                                  join portfolio in Server.Portfolios on position.AccountId equals portfolio.AccountId
                                  where !position.Contragent
                                        && (instrument.RefundLevel == RefundLevelForAll
                                            || (instrument.RefundLevel == RefundLevelForRefundingClients && portfolio.IsRefund))
                                  group position by position.SecCode
                                      into grouped
                                      select new PositionSummary
                                      {
                                          InstrumentCode = grouped.Key,
                                          Quantity = grouped.Sum(g => g.Balance) *
                                                    (from instrument in Server.Instruments
                                                     where instrument.SecCode == grouped.First().SecCode
                                                     select instrument.Lotsize).First(),
                                          MarketType = MarketType.MMA
                                      };

            return positionSummary.ToList();
        }

        /// <summary>
        /// Загрузка позиций Этны
        /// </summary>
        /// <returns></returns>
        private static List<PositionSummary> LoadETNAPositions()
        {
            var command = new Command
            {
                CommandText = "GetPositionsSummary",
                Data = GetSessionStartedEtnaPositions()
            };

            List<ETNAPosition> etnaPositions;
            try
            {
                // вызов расширения "Этна" для загрузки данных
                etnaPositions = ServerBase.Current.AddIns[Server.ETNAAddInName].Execute(command) as List<ETNAPosition>;
            }
            catch (Exception e)
            {
                WriteErrorLog("Can't load ETNA addIn " + e);
                return null;
            }

            if (etnaPositions == null)
                return null;

            // конвертация в ReconciliationPosition и возврат
            return etnaPositions.Select(s => new PositionSummary
            {
                ExternalInstrumentCode = s.EtnaInstumentCode,
                Quantity = s.Quantity,
                MarketType = MarketType.ETNA
            }).ToList();
        }

        /// <summary>
        /// Добавляет данные по позициям Этны на момент начала торговой сессии
        /// </summary>
        private static List<ETNAPosition> GetSessionStartedEtnaPositions()
        {
            return ServerBase.Current.DataBase.GetEtnaOpenPositions().ToList();
        }

        /// <summary>
        /// Загрузка RRM позиций
        /// </summary>
        /// <returns></returns>
        private static List<PositionSummary> LoadRRMPositions()
        {
            // вызываем хранимку
            var externalPositions = ServerBase.Current.DataBase.GetRRMPositionsSummary();

            // хранимка возвращает два датасета. Первый читаем только для того, чтобы добраться до второго
            externalPositions.GetResult<ExternalPosition>();

            // второй возвращаем
            var result = externalPositions.GetResult<ExternalPosition>().ToList();

            // конвертация в PositionSummary и возврат
            return result.GroupBy(position => position.Code).Select(g => new PositionSummary
            {
                Quantity = g.Sum(p => p.Quantity),
                ExternalInstrumentCode = g.Key,
                MarketType = MarketType.RRM
            }).ToList();
        }

        /// <summary>
        /// Загрузка FORTS позиций
        /// </summary>
        /// <returns></returns>
        private static List<PositionSummary> LoadFORTSPositions()
        {
            // вызываем хранимку
            var externalPositions = ServerBase.Current.DataBase.GetFORTSPositionsSummary();

            // хранимка возвращает два датасета. Первый читаем, второй не нужен
            var result = externalPositions.GetResult<FORTSPosition>();

            // конвертация в PositionSummary и возврат
            return result.Select(s => new PositionSummary
            {
                Quantity = s.Quantity,
                ExternalInstrumentCode = s.CodeForMapping,
                MarketType = MarketType.FORTS
            }).ToList();
        }

        /// <summary>
        /// Выполняет сверку
        /// </summary>
        /// <param name="mmaPositions"></param>
        /// <param name="externalPositions"></param>
        /// <returns></returns>
        private static List<ReconciliationPosition> MakeReconciliation(List<PositionSummary> mmaPositions,
            List<PositionSummary> externalPositions)
        {
            var reconciliationData = new List<ReconciliationPosition>();

            // проходим по всем позициям ММА и подсчитываем соответсвующее количество во внешних позициях
            foreach (var mmaPosition in mmaPositions)
            {
                // поулчаем инструмент по коду 
                var instrument = Server.Instruments.FirstOrDefault(s => s.SecCode == mmaPosition.InstrumentCode);

                if (instrument == null)
                {
                    WriteErrorLog("Can't find instrument with code " + mmaPosition.InstrumentCode);
                    continue;
                }

                // получаем объект из коллекции externalPositions
                var externalPosition = GetExternalPositionByInstrumentExternalCode(externalPositions, instrument);

                // позиции нет на внешних рынках - добавляем в результат с QuantityExternal = 0
                if (externalPosition == null)
                {
                    // добавляем результат сверки в выходной объект
                    reconciliationData.Add(new ReconciliationPosition
                    {
                        InstrumentCode = instrument.SecCode,
                        InstrumentName = instrument.Name,
                        QuantityMMA = mmaPosition.Quantity,
                        QuantityExternal = 0,
                        ExternalMarketCode = GetMarketTypeCodeByInstument(instrument),
                    });
                    continue;
                }

                // подсчитываем количество в позиции по инструменту во внешней позиции
                var quantityExternal = GetExternalPositionQuantity(externalPosition, instrument);

                // убираем этот элемент из коллекции внешних позициий
                RemoveExternalPositionElementFromList(externalPositions, externalPosition);

                // добавляем результат сверки в выходной объект
                reconciliationData.Add(new ReconciliationPosition
                {
                    InstrumentCode = instrument.SecCode,
                    InstrumentName = instrument.Name,
                    QuantityMMA = mmaPosition.Quantity,
                    QuantityExternal = quantityExternal,
                    ExternalMarketCode = ToClientString(externalPosition.MarketType),
                });
            }

            // теперь в списке внешних позиций остались только те элементы, которых нет в списке ММА позиций
            reconciliationData.AddRange(GetExternalPositionsExtra(externalPositions));

            reconciliationData.ForEach(s => s.Difference = s.QuantityMMA - s.QuantityExternal);

            return reconciliationData;
        }

        /// <summary>
        /// Добавляет оставшиеся в externalPositions элементы в результат сверки
        /// </summary>
        /// <param name="externalPositions"></param>
        private static List<ReconciliationPosition> GetExternalPositionsExtra(List<PositionSummary> externalPositions)
        {
            var extraPositions = new List<ReconciliationPosition>();

            if (externalPositions == null || !externalPositions.Any())
                return extraPositions;

            // теперь в списке внешних позиций остались только те элементы, которых нет в списке ММА позиций
            // добавляем их в результат сверки с количеством 0 для ММА рынка
            foreach (var externalPosition in externalPositions)
            {
                if (NotReconcilingExternalInstrumentCodes.Contains(externalPosition.ExternalInstrumentCode))
                    continue;

                var instrument = GetInstrumentByExternalCode(externalPosition.ExternalInstrumentCode, externalPosition.MarketType);
                if (instrument == null)
                {
                    WriteErrorLog("Can't find instrument with external code "
                                    + externalPosition.ExternalInstrumentCode + "/" + externalPosition.MarketType);
                }

                var instrumentCode = instrument != null ? instrument.SecCode : string.Empty;
                var instrumentName = instrument != null ? instrument.Name : string.Empty;
                var message = instrument != null ? string.Empty : externalPosition.ExternalInstrumentCode;

                extraPositions.Add(new ReconciliationPosition
                {
                    InstrumentCode = instrumentCode,
                    InstrumentName = instrumentName,
                    QuantityMMA = 0,
                    QuantityExternal = GetExternalPositionQuantity(externalPosition, instrument),
                    ExternalMarketCode = ToClientString(externalPosition.MarketType),
                    Message = string.IsNullOrEmpty(message) ? string.Empty : message + " инструмент не найден"
                });
            }

            return extraPositions;
        }

        /// <summary>
        /// Удаляет внешнюю позицию из списка
        /// </summary>
        /// <param name="externalPositions"></param>
        /// <param name="externalPosition"></param>
        private static void RemoveExternalPositionElementFromList(List<PositionSummary> externalPositions, PositionSummary externalPosition)
        {
            if (externalPosition == null || externalPositions == null)
                return;

            externalPositions.Remove(externalPosition);
        }

        /// <summary>
        /// Возвращает количество внешней позиции с учетом размера лота инструмента
        /// </summary>
        /// <param name="externalPosition"></param>
        /// <param name="instrument"></param>
        /// <returns></returns>
        private static decimal GetExternalPositionQuantity(PositionSummary externalPosition, Instrument instrument)
        {
            if (externalPosition == null)
                return 0;

            if (externalPosition.MarketType == MarketType.ETNA)
            {
                if (instrument != null)
                    return externalPosition.Quantity * instrument.Lotsize;
                else
                    return externalPosition.Quantity;
            }

            return externalPosition.Quantity;
        }

        /// <summary>
        /// Вовзращает внешнюю позицию из списка по ExternalCode инструмента
        /// </summary>
        /// <param name="externalPositions"></param>
        /// <param name="instrument"></param>
        /// <returns></returns>
        private static PositionSummary GetExternalPositionByInstrumentExternalCode(List<PositionSummary> externalPositions,
            Instrument instrument)
        {
            if (instrument == null || externalPositions == null)
                return null;

            return externalPositions.FirstOrDefault(s => s.ExternalInstrumentCode == instrument.ExternalCode
                                                    && (int)s.MarketType == instrument.Market);
        }

        /// <summary>
        /// Возвращает инструмент по внешнему коду инструмента
        /// </summary>
        /// <param name="instrumentExternalCode"></param>
        /// <param name="marketType"></param>
        /// <returns></returns>
        private static Instrument GetInstrumentByExternalCode(string instrumentExternalCode, MarketType marketType)
        {
            var marketTypeId = (int)marketType;
            var instrument = Server.Instruments.FirstOrDefault(s => s.ExternalCode == instrumentExternalCode && s.Market == marketTypeId);
            return instrument;
        }

        /// <summary>
        /// Возвращает код типа рынка по инструменту
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns></returns>
        private static string GetMarketTypeCodeByInstument(Instrument instrument)
        {
            if (instrument == null)
                return string.Empty;

            var marketType = (MarketType)instrument.Market;
            return ToClientString(marketType);
        }

        /// <summary>
        /// Пишет сообщение об ошибке в лог
        /// </summary>
        /// <param name="message"></param>
        private static void WriteErrorLog(string message)
        {
            LogManager.GetCurrentClassLogger().Error("Reconciliation: " + message);
        }

        /// <summary>
        /// Конвертация типа рынка в строку для представления в клиенте
        /// </summary>
        /// <param name="marketType">Тип рынка</param>
        /// <returns></returns>
        private static string ToClientString(MarketType marketType)
        {
            switch (marketType)
            {
                case MarketType.ETNA:
                    return "USA";
                case MarketType.RRM:
                    return "MICEX";
                case MarketType.FORTS:
                    return "FORTS";
            }
            return marketType.ToString();
        }

        /// <summary>
        /// Суммарная информация по позиции
        /// </summary>
        private class PositionSummary
        {
            /// <summary>
            /// Код инструмента
            /// </summary>
            public string InstrumentCode { get; set; }

            /// <summary>
            /// Внешний код инструмента
            /// </summary>
            public string ExternalInstrumentCode { get; set; }

            /// <summary>
            /// Количество
            /// </summary>
            public decimal Quantity { get; set; }

            /// <summary>
            /// Тип рынка
            /// </summary>
            public MarketType MarketType { get; set; }
        }

        /// <summary>
        /// Тип рынка
        /// </summary>
        private enum MarketType
        {
            MMA = 14,
            ETNA = 12,
            RRM = 1,
            FORTS = 4
        }
    }

    // todo Нужно куда-нибудь вынести, используется в Database.cs
    /// <summary>
    /// Внешняя позиция
    /// </summary>
    public class ExternalPosition
    {
        /// <summary>
        /// Код инструмента
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Суммарная позиция по инструменту в единицах на основании размера лота инструмента
        /// </summary>
        public decimal Quantity { get; set; }
    }

    /// <summary>
    /// Внешняя позиция FORTS
    /// </summary>
    public class FORTSPosition
    {
        /// <summary>
        /// Код инструмента
        /// </summary>
        [Column(Name = "ISIN")]
        public string Code { get; set; }

        /// <summary>
        /// Код инструмента 
        /// </summary>
        [Column(Name = "ISIN:Short")]
        public string CodeForMapping { get; set; }

        /// <summary>
        /// Суммарная позиция по инструменту в единицах на основании размера лота инструмента
        /// </summary>
        [Column(Name = "Pos")]
        public int Quantity { get; set; }


        /// <summary>
        /// Суммарная позиция по инструменту в единицах на основании размера лота инструмента
        /// </summary>
        [Column(Name = "PosGO")]
        public decimal GoPos { get; set; }
    }
}
