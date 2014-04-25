using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Risk
{
    /// <summary>
    /// Таблица портфелей
    /// </summary>
    [Table("Portfolios", KeyFields = "TradeCode")]
    public class Portfolios : Table<Portfolio>
    {
        public override void TriggerAfter(TriggerCollection<Portfolio> items)
        {
            // Пересчет оборота в USD
            ApplyRates(Server.ExchangeRates, items.Updated);

            // TODO: Вынести в правила
            CheckRules(items.Updated);
        }

        private void CheckRules(IEnumerable<Portfolio> items)
        {
            // ВИ 3. ФТ 8. 
            // Требуется создавать оповещения для Пользователя при наступлении следующих событий 
            // в соответствии с выбранными настройками (см. ФТ 26):
            //  - Превышение прибыли ;
            //  - Превышение % прибыли относительно входящего капитала;
            //  - Превышение оборота в USD по сделкам;
            //  - Превышение % оборота в USD по сделкам относительно входящего капитала.

            var ruleItems = new List<PortfolioRule>();
            foreach (var p in items)
            {
                #region Проверка прибыли и оборотов

                if (Server.Settings.NotifyAdmin)
                {
                    //  - Превышение прибыли ;
                    if (p.PLCurrencyCalc > 0 && 
                        Server.Settings.MaxSumProfit > 0 &&
                        p.PLCurrencyCalc >= Server.Settings.MaxSumProfit)
                    {
                        ruleItems.Add(new PortfolioRule
                        {
                            RuleType = RuleType.MaxProfitExceed,
                            Portfolio = p,
                            RuleTime = Server.Current.ServerTime,
                        });
                    }

                    //  - Превышение % прибыли относительно входящего капитала;
                    if (p.PL > 0 && p.OpenBalance > 0 &&
                        Server.Settings.MaxPercentProfit > 0 &&
                        p.PL / p.OpenBalance * 100 >= Server.Settings.MaxPercentProfit)
                    {
                        ruleItems.Add(new PortfolioRule
                        {
                            RuleType = RuleType.MaxPercentProfitExceed,
                            Portfolio = p,
                            RuleTime = Server.Current.ServerTime,
                        });
                    }

                    //  - Превышение оборота в USD по сделкам;
                    if (p.TurnoverCurrencyCalc > 0 && 
                        Server.Settings.MaxSumTurnover > 0 &&
                        p.TurnoverCurrencyCalc >= Server.Settings.MaxSumTurnover)
                    {
                        ruleItems.Add(new PortfolioRule
                        {
                            RuleType = RuleType.MaxTurnoverExceed,
                            Portfolio = p,
                            RuleTime = Server.Current.ServerTime,
                        });
                    }

                    //  - Превышение % оборота в USD по сделкам относительно 
                    // входящего капитала.
                    if (p.Turnover > 0 &&
                        p.OpenBalance > 0 &&
                        Server.Settings.MaxPercentTurnover > 0 &&
                        p.Turnover / p.OpenBalance * 100 >= Server.Settings.MaxPercentTurnover)
                    {
                        ruleItems.Add(new PortfolioRule
                        {
                            RuleType = RuleType.MaxPercentTurnoverExceed,
                            Portfolio = p,
                            RuleTime = Server.Current.ServerTime,
                        });
                    }
                }

                #endregion

                #region Оповещение пользователей о Margin Call
                    
                if (Server.Settings.MarginForceClose)
                {
                    if (p.UtilizationFact > 0)
                    {
                        // Превышение % использования капитала, 
                        // при котором должны быть закрыты позиции
                        if (Server.Settings.MaxPercentUtilMarginCall > 0 &&
                        p.UtilizationFact >= Server.Settings.MaxPercentUtilMarginCall)
                        {
                            // Ставим признак в портфеле Margin Call
                            p.MarginCall = true;

                            // Отправка оповещения пользователю
                            ruleItems.Add(new PortfolioRule
                            {
                                RuleType = RuleType.MaxPercentUtilMarginCallExceed,
                                Portfolio = p,
                                RuleTime = Server.Current.ServerTime,
                            });
                        }
                        // Превышение % использования капитала, 
                        // при которой отправляется предупреждение клиенту
                        if (Server.Settings.NotifyClientMaxPercentUtilExceed &&
                            Server.Settings.MaxPercentUtilWarning > 0 &&
                            p.UtilizationFact >= Server.Settings.MaxPercentUtilWarning)
                        {
                            // Отправка оповещения пользователю и клиенту
                            ruleItems.Add(new PortfolioRule
                            {
                                RuleType = RuleType.MaxPercentUtilWarningExceed,
                                Portfolio = p,
                                RuleTime = Server.Current.ServerTime,
                            });
                        }
                    }
                }

                #endregion

                // Снятие Margin Call в случае изменения ситуации
                if (p.MarginCall)
                {
                    if (!Server.Settings.MarginForceClose ||
                        (Server.Settings.MaxPercentUtilMarginCall > 0 &&
                        p.UtilizationFact < Server.Settings.MaxPercentUtilMarginCall))
                    {
                        p.MarginCall = false;
                    }
                }
            }

            if (ruleItems.Count > 0)
            {
                new CommandMerge 
                {
                    Object = Server.PortfolioRules,
                    Data = ruleItems,   
                    KeyFields = "Portfolio",
                    Fields = "RuleTime",
                }.ExecuteAsync();                                        
            }
        }
       
        private void ApplyRates(IEnumerable<Rate> rates, IEnumerable<Portfolio> items = null)
        {
            //var updateItems = items ?? _items;
            // TODO: Пересчет оборота в USD
            foreach (var port in from p in items
                                 join r in rates on new { CurrencyFrom = p.Currency, CurrencyTo = "USD" } equals new { r.CurrencyFrom, r.CurrencyTo } into ps
                                 from r in ps.DefaultIfEmpty()
                                 select new { Portfolio = p, Rate = r })
            {
                if (port.Portfolio.Currency != null && port.Portfolio.Currency.Equals(Server.Settings.CurrencyCalc))
                {
                    port.Portfolio.TurnoverCurrencyCalc = port.Portfolio.Turnover;
                    port.Portfolio.CapitalCurrencyCalc = port.Portfolio.Capital;
                    port.Portfolio.PLCurrencyCalc = port.Portfolio.PL;
                }
                else if (port.Rate != null)
                {
                    port.Portfolio.TurnoverCurrencyCalc = port.Portfolio.Turnover * port.Rate.Value;
                    port.Portfolio.CapitalCurrencyCalc = port.Portfolio.Capital * port.Rate.Value;
                    port.Portfolio.PLCurrencyCalc = port.Portfolio.PL * port.Rate.Value;
                }
            }          

        }
    }
}