using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NLog;

namespace Risk
{
    /// <summary>
    /// Таблица сделок
    /// </summary>
    [Table("Trades", KeyFields = "TradeNo")]
    public class Trades : Table<Trade>
    {

        /// <summary>
        /// Лог
        /// </summary>
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Список таймеров
        /// </summary>
        private readonly List<System.Timers.Timer> _timers = new List<System.Timers.Timer>();

        /// <summary>
        /// Задержка проверки наличия позиции по сделке
        /// </summary>
        private const int CheckTradeCorrespondingPositionDelay = 10 * 1000;

        /// <summary>
        /// Временной интервал, при превышении которого сделка считается устаревшей
        /// </summary>
        private const int TradeObsolescenceInterval = 60 * 1000;

        /// <summary>
        /// GetChanges
        /// </summary>
        /// <returns></returns>
        protected override NotificationData<Trade> GetChanges()
        {
            return null; // Не посылаем обновления по сделкам
        }

        /// <summary>
        /// TriggerAfter
        /// </summary>
        /// <param name="items"></param>
        public override void TriggerAfter(TriggerCollection<Trade> items)
        {
            ProcessItems(items);
        }

        /// <summary>
        /// Обработка новых элементов
        /// </summary>
        /// <param name="items"></param>
        private void ProcessItems(TriggerCollection<Trade> items)
        {
            SetInstrumentData(items.Inserted);

            // Пересчет суммы в сделке с учетом bpcost и lotsize
            CalcSum(Server.Instruments, items.Updated);

            // обработка портфелей 
            UpdatePortfoliosTurnover(items);

            // обработка позиций
            ProcessPositions(items);
        }

        /// <summary>
        /// Обработка позиций
        /// </summary>
        /// <param name="items"></param>
        private void ProcessPositions(TriggerCollection<Trade> items)
        {
            // группируем трейды по торговому коду и коду инстурмента, т.к. по одной заявке может произойти несколько сделок.
            // Более того, эти сделки из транзака могут прийти в совершенно разных пакетах.
            var positions = from t in items
                            group t by new { t.Updated.TradeCode, t.Updated.SecCode }
                                into g
                                select new
                                {
                                    Position = new Position
                                   {
                                       TradeCode = g.Key.TradeCode,
                                       SecCode = g.Key.SecCode,
                                       DealsCount = g.Count(),
                                       Turnover = g.CumulativeSum(t => t.ValueCalc),
                                       TurnoverQuantity = g.CumulativeSum(t => t.Quantity)
                                   },
                                    UpdateDate = g.FirstOrDefault().Updated.TradeTime,
                                    TradeId = g.FirstOrDefault().Updated.TradeNo,
                                    OrderId = g.FirstOrDefault().Updated.OrderNo
                                };

            // смысл в том, что сделка уже может из транзака прийти, а позиция по ней - нет
            // в таком случае нужно дождать прихода сделки из транзака и проапдейтить ее поля. 
            // И сохранить статистику
            foreach (var position in positions)
            {
                WaitForTradeCorrespondingPosition(position.Position, position.UpdateDate, position.TradeId, position.OrderId);
            }
        }

        /// <summary>
        /// Обработка портфелей
        /// </summary>
        /// <param name="items"></param>
        private static void UpdatePortfoliosTurnover(TriggerCollection<Trade> items)
        {
            // Расчет оборотов по сделкам в разрезе счетов
            var portfolios = (from t in items
                              group t by t.Updated.TradeCode
                                  into g
                                  select new Portfolio
                                  {
                                      TradeCode = g.Key,
                                      Turnover = g.CumulativeSum(t => t.ValueCalcInPortfolioCurrency)
                                  }).ToList();
            new CommandUpdate
            {
                Object = Server.Portfolios,
                Data = portfolios,
                Fields = "@Turnover",
            }.ExecuteAsync();
        }

        /// <summary>
        /// Ожидание появления позиции и обновление ее полей
        /// </summary>
        /// <param name="position"></param>
        /// <param name="tradeTime"></param>
        /// <param name="tradeId"></param>
        /// <param name="orderId"></param>
        private void WaitForTradeCorrespondingPosition(Position position, DateTime tradeTime, long tradeId, long orderId)
        {
            var canSaveStatistic = CanSaveClientTradingStatistic(tradeTime, tradeId);

            var timer = new System.Timers.Timer(CheckTradeCorrespondingPositionDelay);

            // готовим таймер
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop(); // остановка и удаление таймера
                _timers.Remove(timer);

                // проверяем, появилась ли позиция
                if (Server.Positions.Any(p => p.SecCode == position.SecCode && p.TradeCode == position.TradeCode))
                {
                    try
                    {
                        // появилась - обновляем ее поля 
                        new CommandUpdate
                        {
                            Object = Server.Positions,
                            Data = new List<Position> { position },
                            Fields = "@DealsCount,@Turnover,@TurnoverQuantity",
                        }.ExecuteAsync();

                        // сохраняем статистику
                        if (canSaveStatistic)
                            SaveClientTradingStatistic(position.TradeCode, tradeTime, tradeId, orderId);
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Unable to save client trading statistic for trade {0} {1}", tradeId, e);
                    }
                }
                else
                {
                    _logger.Warn("No position found for trade {0}", tradeId);
                }
            };

            _timers.Add(timer);
            timer.Start();
        }

        /// <summary>
        /// Определяет можем мы или нет сохранять статистику для этой сделки
        /// </summary>
        /// <param name="tradeTime"></param>
        /// <param name="tradeId"></param>
        /// <returns></returns>
        private bool CanSaveClientTradingStatistic(DateTime tradeTime, long tradeId)
        {
            // выравниваем время сделки и время сервера
            tradeTime = tradeTime.Add(TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow));

            // получаем разницу между временем сделки и временем сервера
            var timeDifference = new TimeSpan(Server.Settings.ServerTime.Ticks - tradeTime.Ticks);

            // если время устаревания не превышено, можем сохранять статистику
            if (timeDifference.TotalMilliseconds <= TradeObsolescenceInterval)
                return true;

            if (timeDifference.TotalMilliseconds < TradeObsolescenceInterval * 5)
                _logger.Warn("Obsolete trade {0} time difference < 5 min", tradeId);

            return false;
        }

        /// <summary>
        /// Сохраняет торговую статистику клиента
        /// </summary>
        /// <param name="tradeCode"></param>
        /// <param name="updateDate"></param>
        /// <param name="tradeId"></param>
        /// <param name="orderId"></param>
        private static void SaveClientTradingStatistic(string tradeCode, DateTime updateDate, long tradeId, long orderId)
        {
            // получаем портфель и позицию по текущей сделке
            var positions = Server.Positions.Where(s => s.TradeCode == tradeCode).ToList();
            var portfolio = Server.Portfolios.SingleOrDefault(s => s.TradeCode == tradeCode);

            if (portfolio == null || portfolio.Contragent)
                return;

            // вызов хранимки по сохранению в БД для всех позиций в портфеле клиента
            foreach (var position in positions)
            {
                ServerBase.Current.DataBase.SaveClientTradingStatistic(tradeId, orderId, updateDate, string.Empty, portfolio.TradeCode,
                portfolio.Client, portfolio.Currency, portfolio.Capital, portfolio.CoverageFact, portfolio.UtilizationFact,
                position.SecCode, position.SecurityCurrency, position.Quote, position.OpenBalance, position.Bought, position.Sold,
                position.Balance, position.PL);
            }
        }

        /// <summary>
        /// GetData
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override object GetData(ParameterCollection parameters)
        {
            var items = ((IEnumerable<Trade>)base.GetData(parameters)).ToList();

            DateTime dateFrom, dateTo;

            Alerts.GetDatesParameters(parameters, out dateFrom, out dateTo);

            // если даты не указаны, то возвращаем оригинальную коллекцию
            if (dateFrom == DateTime.MinValue && dateTo == DateTime.MaxValue)
                return items.ToArray();

            // загружаем сделки из БД
            var trades = ServerBase.Current.DataBase.LoadTrades(dateFrom, dateTo).ToList();

            // применяем предикат, если он заполнен
            var predicate = Predicate(parameters);

            if (predicate != null)
                trades = trades.Select(trade => trade).Where(predicate.Compile()).ToList();

            SetInstrumentData(trades);

            items.AddRange(trades);

            return items.ToArray();
        }

        /// <summary>
        /// Предикат
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override Expression<Func<Trade, bool>> Predicate(ParameterCollection parameters)
        {
            var predicate = base.Predicate(parameters);

            DateTime dateFrom, dateTo;

            // получаем даты из параметров
            Alerts.GetDatesParameters(parameters, out dateFrom, out dateTo);

            // добавляем фильтр в предикат если хотя бы одна дата заполена
            if (dateFrom != DateTime.MinValue || dateTo != DateTime.MaxValue)
                predicate = predicate.And(s => s.TradeTime.Date >= dateFrom && s.TradeTime.Date <= dateTo);

            return predicate;
        }

        /// <summary>
        /// Заполенние дополнительных данных для инструмента
        /// </summary>
        /// <param name="items"></param>
        public static void SetInstrumentData(IEnumerable<Trade> items)
        {
            if (items == null)
                return;

            foreach (var item in items)
            {
                var instrument = Server.Instruments.FirstOrDefault(s => s.SecCode == item.SecCode);
                if (instrument == null)
                    continue;
                item.InstrumentName = instrument.Name;
                item.InstrumentClassCode = instrument.ClassCode;
                item.InstrumentClassName = instrument.ClassName;
            }
        }

        /// <summary>
        /// Пересчет суммы в сделке с учетом bpcost и lotsize
        /// </summary>
        private void CalcSum(IEnumerable<Instrument> instruments, IEnumerable<Trade> items = null)
        {
            // http://it-portal/tasks/browse/FMD-864

            //  b. Если lotsize в тикере МСТ = NULL (отсутствует), то
            //      Количество = количество в сделке (из БД Транзак)
            //      Сумма = Количество * Цена (с учетом формата цены) * bpcost / 100 000
            foreach (var t in from tr in items
                              join i in instruments on tr.SecCode equals i.SecCode into tri
                              from p in tri.DefaultIfEmpty()
                              select new { Trade = tr, Instrument = p })
            {
                if (t.Instrument == null)
                {
                    t.Trade.ValueCalc = 0;
                }
                else
                {
                    t.Trade.ValueCalc = new decimal(t.Trade.Price * t.Trade.Quantity * Math.Pow(10, t.Instrument.Decimals) * t.Instrument.Bpcost / 100000);

                    var tradePosition = Server.Positions.FirstOrDefault(s => s.TradeCode == t.Trade.TradeCode && s.SecCode == t.Trade.SecCode);

                    var crossCourse = CalculateCrossCourse(t.Trade, tradePosition, t.Instrument.SecurityCurrency);
                    t.Trade.ValueCalcInPortfolioCurrency = t.Trade.ValueCalc * crossCourse;
                }
            }
        }

        /// <summary>
        /// Вычисление курса валюты инструмента к валюте портфеля
        /// </summary>
        /// <param name="trade"></param>
        /// <param name="position"></param>
        /// <param name="instrumentCurrency"></param>
        /// <returns></returns>
        private static decimal CalculateCrossCourse(Trade trade, Position position, string instrumentCurrency)
        {
            var crossRate = 0m;

            // берем курс из позиции
            if (position != null)
            {
                crossRate = position.CrossRate;
            }

            if (crossRate != 0)
                return crossRate;

            // если в позиции курса нет или нет самой позиции, то получаем портфель клиента
            var portfolio = Server.Portfolios.SingleOrDefault(s => s.TradeCode == trade.TradeCode);

            if (portfolio == null)
                return crossRate;

            // ищем кросс курс валюты инстурмента к валюте портфеля
            var rate = Server.ExchangeRates.SingleOrDefault(s => s.CurrencyFrom == instrumentCurrency && s.CurrencyTo == portfolio.Currency);

            if (rate != null)
                crossRate = rate.Value;

            return crossRate;
        }
    }
}