using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Risk
{
    /// <summary>
    /// Таблица позиций
    /// </summary>
    [Table("Positions", KeyFields = "TradeCode,SecCode")]
    public class Positions : Table<Position>
    {
        protected override Expression<Func<Position, bool>> Predicate(ParameterCollection parameters)
        {
            var predicate = base.Predicate(parameters);

            // TODO: !!! Передавать с клиента
            parameters["ExcludeContragent"] = true;

            // необходимо НЕ учитывать счета центрального контрагента
            var excludeContragent = object.Equals(parameters["ExcludeContragent"], true);
            if (excludeContragent)
                predicate = predicate.And(x => !x.Contragent);
            return predicate;
        }

        public override void TriggerAfter(TriggerCollection<Position> items)
        {

            SetFinancialResultData(items);

            SetInstrumentData(items);

            // Пересчет оборота в валюту расчетов
            ApplyRates(Server.ExchangeRates, Server.Portfolios, items.Updated.Where(x => ((x.Turnover != 0) || (x.PL != 0) || (x.FinRes != 0))));//.ToList());

            // Группировка по инструменту
            var positionsInstruments = (from pair in items
                                        where pair.Updated.Contragent == false
                                        group pair by new { SecCode = pair.Updated.SecCode, pair.Updated.SecurityCurrency } into g
                                        select new Position
                                        {
                                            SecCode = g.Key.SecCode,
                                            SecurityCurrency = g.Key.SecurityCurrency,

                                            // Из портфеля
                                            OpenBalance = g.CumulativeSum(x => x.OpenBalance),
                                            Bought = g.CumulativeSum(x => x.Bought),
                                            Sold = g.CumulativeSum(x => x.Sold),
                                            Balance = g.CumulativeSum(x => x.Balance),
                                            PLCurrencyCalc = g.CumulativeSum(x => x.PLCurrencyCalc),
                                            PLCurrencyDisplay = g.CumulativeSum(x => x.PLCurrencyDisplay),

                                            // Из сделки
                                            DealsCount = g.CumulativeSum(x => x.DealsCount),
                                            Turnover = g.CumulativeSum(x => x.Turnover),
                                            TurnoverCurrencyCalc = g.CumulativeSum(x => x.TurnoverCurrencyCalc),
                                            TurnoverCurrencyDisplay = g.CumulativeSum(x => x.TurnoverCurrencyDisplay),
                                            TurnoverQuantity = g.CumulativeSum(x => x.TurnoverQuantity),
                                            FinResCurrencyDisplay = CalculateInstrumentFinResCurrencyDisplay(g.Key.SecCode),
                                            PositionCost = g.CumulativeSum(x => ConvertPositionCostToInstrumentCurrency(g.Key.SecurityCurrency, x.TradeCode, x.PositionCost))
                                        }).ToList();

            if (positionsInstruments.Count > 0)
            {
                new CommandMerge
                {
                    Object = Server.PositionsInstruments,
                    Data = positionsInstruments,
                    Fields = "@OpenBalance,@Bought,@Sold,@Balance,@PL,@PLCurrencyDisplay,@DealsCount,@Turnover,@TurnoverQuantity,@TurnoverCurrencyDisplay,FinResCurrencyDisplay,@PositionCost",
                    KeyFields = "SecCode",
                }.ExecuteAsync();
            }
        }

        /// <summary>
        /// Конвертация стоимости позиции в валюту инструмента
        /// </summary>
        /// <param name="securityCurrency"></param>
        /// <param name="tradeCode"></param>
        /// <param name="positionCost"></param>
        /// <returns></returns>
        private static decimal ConvertPositionCostToInstrumentCurrency(string securityCurrency, string tradeCode, decimal positionCost)
        {
            var portfolio = Server.Portfolios.SingleOrDefault(s => s.TradeCode == tradeCode);
            if (portfolio == null)
                return 0;

            var rate = Server.ExchangeRates.FirstOrDefault(s => s.CurrencyFrom == portfolio.Currency && s.CurrencyTo == securityCurrency);
            if (rate == null)
                return 0;
            return positionCost * rate.Value;
        }

        /// <summary>
        /// Расчет финансового результата в валюте отображения для инструмента
        /// </summary>
        /// <param name="instumentCode"></param>
        /// <returns></returns>
        private static decimal CalculateInstrumentFinResCurrencyDisplay(string instumentCode)
        {
            // группируем по коду инструмента
            var groupedFinRes = Server.FinancialResults.GroupBy(s => s.SecCode).SingleOrDefault(d => d.Key == instumentCode);

            // и возвращаем сумму финансового результата
            return groupedFinRes != null ? groupedFinRes.Sum(s => s.FinResCurrencyDisplay) : 0;
        }

        /// <summary>
        /// Добавляет в поля таблицы значения финансового результата
        /// </summary>
        /// <param name="items"></param>
        private void SetFinancialResultData(TriggerCollection<Position> items)
        {
            // задержка на случай прихода данных по позициям раньше, чем отработает хранимка финрез
            var financialResultsTable = ServerBase.Current.FindDataObject("FinancialResults") as FinancialResults;
            if (financialResultsTable != null)
            {
                var i = 60 * 5;
                while (!financialResultsTable.Any() && i > 0)
                {
                    Thread.Sleep(1000);
                    i--;
                }
            }

            // добавляем данные во вставленные впервые позиции
            foreach (var position in items.Inserted)
            {
                var finRes = Server.FinancialResults.SingleOrDefault(s => s.TradeCode == position.TradeCode && s.SecCode == position.SecCode);
                if (finRes == null)
                    continue;

                position.FinResCurrencyDisplay = finRes.FinResCurrencyDisplay;
                position.FinRes = finRes.FinRes;
            }
        }

        public static void SetInstrumentData(TriggerCollection<Position> items)
        {
            foreach (var item in items.Inserted)
            {
                var instrument = Server.Instruments.SingleOrDefault(s => s.SecCode == item.SecCode);
                if (instrument == null)
                    continue;
                item.InstrumentName = instrument.Name;
                item.InstrumentClassCode = instrument.ClassCode;
                item.InstrumentClassName = instrument.ClassName;
            }
        }

        public static void ApplyRates(IEnumerable<Rate> rates, IEnumerable<Portfolio> portfolios, IEnumerable<Position> items = null)
        {
            foreach (var port in from p in items
                                 join pf in portfolios on p.TradeCode equals pf.TradeCode
                                 join r in rates on new { CurrencyFrom = pf.Currency, CurrencyTo = Server.Settings.CurrencyCalc } equals new { r.CurrencyFrom, r.CurrencyTo } into ps
                                 from r in ps.DefaultIfEmpty()
                                 select new { Position = p, Currency = pf.Currency, Rate = r })
            {
                if (port.Currency.Equals(Server.Settings.CurrencyCalc))
                {
                    port.Position.TurnoverCurrencyCalc = port.Position.Turnover;
                    port.Position.PLCurrencyCalc = port.Position.PL;
                }
                else if (port.Rate != null)
                {
                    port.Position.TurnoverCurrencyCalc = port.Position.Turnover * port.Rate.Value;
                    port.Position.PLCurrencyCalc = port.Position.PL * port.Rate.Value;
                }
            }

            foreach (var port in from p in items
                                 join pf in portfolios on p.TradeCode equals pf.TradeCode
                                 join r in rates on new { CurrencyFrom = pf.Currency, CurrencyTo = Server.Settings.CurrencyDisplay }
                                 equals new { r.CurrencyFrom, r.CurrencyTo } into ps
                                 from r in ps.DefaultIfEmpty()
                                 select new { Position = p, Currency = pf.Currency, Rate = r })
            {
                if (port.Currency.Equals(Server.Settings.CurrencyDisplay))
                {
                    port.Position.TurnoverCurrencyDisplay = port.Position.Turnover;
                    port.Position.PLCurrencyDisplay = port.Position.PL;
                }
                else if (port.Rate != null)
                {
                    port.Position.TurnoverCurrencyDisplay = port.Position.Turnover * port.Rate.Value;
                    port.Position.PLCurrencyDisplay = port.Position.PL * port.Rate.Value;
                }
            }

        }
    }
}