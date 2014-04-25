using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
            // Пересчет оборота в USD
            ApplyRates(Server.ExchangeRates, Server.Portfolios, items.Updated.Where(x => x.Turnover != 0));//.ToList());

            // Группировка по инструменту
            var positionsInstruments = (from pair in items
                                       where pair.Updated.Contragent == false
                                       group pair by new { SecCode = pair.Updated.SecCode, pair.Updated.SecurityCurrency } into g
                                       select new Position
                                       {
                                           SecCode = g.Key.SecCode,
                                           SecurityCurrency = g.Key.SecurityCurrency,

                                           // Из портфеля
                                           Bought = g.CumulativeSum(x => x.Bought),
                                           Sold = g.CumulativeSum(x => x.Sold),
                                           Balance = g.CumulativeSum(x => x.Balance),
                                           PL = g.CumulativeSum(x => x.PL),
                                           
                                           // Из сделки
                                           DealsCount = g.CumulativeSum(x => x.DealsCount),
                                           Turnover = g.CumulativeSum(x => x.Turnover),
                                           TurnoverCurrencyCalc = g.CumulativeSum(x => x.TurnoverCurrencyCalc),
                                           TurnoverQuantity = g.CumulativeSum(x => x.TurnoverQuantity),
                                       }).ToList();

            if (positionsInstruments.Count > 0)
            {
                new CommandMerge
                {
                    Object = Server.PositionsInstruments,
                    Data = positionsInstruments,
                    Fields = "@Bought,@Sold,@Balance,@PL,@DealsCount,@Turnover,@TurnoverQuantity,@TurnoverCurrencyCalc",
                    KeyFields = "SecCode",
                }.ExecuteAsync();
            }
        }

        private void ApplyRates(IEnumerable<Rate> rates, IEnumerable<Portfolio> portfolios, IEnumerable<Position> items = null)
        {
            foreach (var port in from p in items
                                 join pf in portfolios on p.TradeCode equals pf.TradeCode
                                 join r in rates on new { CurrencyFrom = pf.Currency, CurrencyTo = "USD" } equals new { r.CurrencyFrom, r.CurrencyTo } into ps
                                 from r in ps.DefaultIfEmpty()
                                 select new { Position = p, Currency = pf.Currency, Rate = r })
            {
                if (port.Currency.Equals(Server.Settings.CurrencyCalc))
                {
                    port.Position.TurnoverCurrencyCalc = port.Position.Turnover;
                }
                else if (port.Rate != null)
                {
                    port.Position.TurnoverCurrencyCalc = port.Position.Turnover * port.Rate.Value;
                }
            }

        }
    }
}