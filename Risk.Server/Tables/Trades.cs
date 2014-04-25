using System;
using System.Collections.Generic;
using System.Linq;

namespace Risk
{
    /// <summary>
    /// Таблица сделок
    /// </summary>
    [Table("Trades", KeyFields = "TradeNo")]
    public class Trades : Table<Trade>
    {

        protected override IEnumerable<NotificationData> GetChanges()
        {
            return null; // Не посылаем обновления по сделкам
        }

        public override void TriggerAfter(TriggerCollection<Trade> items)
        {
            // Пересчет суммы в сделке с учетом bpcost и lotsize
            CalcSum(Server.Instruments, items.Updated);

            // Расчет оборотов по сделкам в разрезе счетов
            var portfolios = from t in items
                             group t by t.Updated.TradeCode into g
                             select new Portfolio
                             {
                                 TradeCode = g.Key,
                                 Turnover = g.CumulativeSum(t => t.ValueCalc)
                             };

            new CommandUpdate
            {
                Object = Server.Portfolios,
                Data = portfolios,
                Fields = "@Turnover",
            }.ExecuteAsync();

            // Расчет оборотов по сделкам в разрезе инструментов
            var positions = from t in items
                            group t by new { t.Updated.TradeCode, t.Updated.SecCode } into g
                            select new Position
                            {
                                TradeCode = g.Key.TradeCode,
                                SecCode = g.Key.SecCode,
                                DealsCount = g.Count(),
                                Turnover = g.CumulativeSum(t => t.ValueCalc),
                                TurnoverQuantity = g.CumulativeSum(t => t.Quantity)
                            };
            new CommandUpdate
            {
                Object = Server.Positions,
                Data = positions,
                Fields = "@DealsCount,@Turnover,@TurnoverQuantity",
            }.ExecuteAsync();
        }
        
        /// <summary>
        /// Пересчет суммы в сделке с учетом bpcost и lotsize
        /// </summary>
        private void CalcSum(IEnumerable<Instrument> instruments, IEnumerable<Trade> items = null)
        {
            // http://it-portal/tasks/browse/FMD-864
            //  a. Если lotsize в тикере МСТ => 1, то 
            //      Количество = количество в сделке (из БД Транзак) * lotsize 
            //      Сумма = Количество * Цена (с учетом формата цены)
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
                    //throw new Exception(String.Format("Не найдены параметры инструмента {0}", t.Trade.SecCode));
                }
                else
                {
                    if (t.Instrument.Lotsize >= 1)
                        t.Trade.ValueCalc = t.Trade.Value * t.Instrument.Lotsize;
                    else
                        t.Trade.ValueCalc = t.Trade.Value * (decimal)Math.Pow(10, t.Instrument.Decimals) * t.Instrument.Bpcost / 100000;
                }
            }
        }
    }
}