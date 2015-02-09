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
        /// <summary>
        /// Список портфелей по которым не устанавливается признак маржин-колла
        /// </summary>
        public static readonly List<string> ContragentTradeCodes = new List<string>
        {
            "MCE0002",
            "MCR0002",
            "MCU0002"
        };

        public override void TriggerAfter(TriggerCollection<Portfolio> items)
        {
            // Пересчет оборота в валюту расчетов
            ApplyRates(Server.ExchangeRates, items.Updated);

            // TODO: Вынести в правила
            CheckRules(items.Updated);
        }

        /// <summary>
        /// Проверка превышения прибыли 
        /// </summary>
        public static bool CheckIfMaxProfitExceed(Portfolio p)
        {
            return (p.PLCurrencyCalc > 0
                && Server.Settings.MaxSumProfit > 0
                && p.PLCurrencyCalc >= Server.Settings.MaxSumProfit);
        }

        /// <summary>
        /// Проверка превышения процента прибыли от входящего капитала
        /// </summary>
        public static bool CheckIfMaxPercentProfitExceed(Portfolio p)
        {
            return (p.PL > 0
                && p.OpenBalance > 0
                && Server.Settings.MaxPercentProfit > 0
                && p.PL / p.OpenBalance * 100 >= Server.Settings.MaxPercentProfit);
        }

        /// <summary>
        /// Проверка превышения оборота по сделкам
        /// </summary>
        public static bool CheckIfMaxTurnoverExceed(Portfolio p)
        {
            return (p.TurnoverCurrencyCalc > 0
                && Server.Settings.MaxSumTurnover > 0
                && p.TurnoverCurrencyCalc >= Server.Settings.MaxSumTurnover);
        }

        /// <summary>
        /// Проверка превышения процента оборота по сделкам от входящего капитала
        /// </summary>
        public static bool CheckIfMaxPercentTurnoverExceed(Portfolio p)
        {
            return (p.Turnover > 0
                && p.OpenBalance > 0
                && Server.Settings.MaxPercentTurnover > 0
                && p.Turnover / p.OpenBalance * 100 >= Server.Settings.MaxPercentTurnover);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        public static void CheckRules(IEnumerable<Portfolio> items)
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

                p.IsMaxProfitExceed = CheckIfMaxProfitExceed(p);
                p.IsMaxPercentProfitExceed = CheckIfMaxPercentProfitExceed(p);
                p.IsMaxTurnoverExceed = CheckIfMaxTurnoverExceed(p);
                p.IsMaxPercentTurnoverExceed = CheckIfMaxPercentTurnoverExceed(p);

                if (Server.Settings.NotifyAdmin)
                {
                    //  - Превышение прибыли ;
                    /*
                    if (p.PLCurrencyCalc > 0 && 
                        Server.Settings.MaxSumProfit > 0 &&
                        p.PLCurrencyCalc >= Server.Settings.MaxSumProfit)
                    */
                    if (p.IsMaxProfitExceed)
                    {
                        ruleItems.Add(new PortfolioRule
                        {
                            RuleType = RuleType.MaxProfitExceed,
                            Portfolio = p,
                            RuleTime = Server.Current.ServerTime,
                        });
                    }

                    //  - Превышение % прибыли относительно входящего капитала;
                    /*
                    if (p.PL > 0 && p.OpenBalance > 0 &&
                        Server.Settings.MaxPercentProfit > 0 &&
                        p.PL / p.OpenBalance * 100 >= Server.Settings.MaxPercentProfit)
                    */
                    if (p.IsMaxPercentProfitExceed)
                    {
                        ruleItems.Add(new PortfolioRule
                        {
                            RuleType = RuleType.MaxPercentProfitExceed,
                            Portfolio = p,
                            RuleTime = Server.Current.ServerTime,
                        });
                    }

                    //  - Превышение оборота в USD по сделкам;
                    /*
                    if (p.TurnoverCurrencyCalc > 0 && 
                        Server.Settings.MaxSumTurnover > 0 &&
                        p.TurnoverCurrencyCalc >= Server.Settings.MaxSumTurnover)
                    */
                    if (p.IsMaxTurnoverExceed)
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
                    /*
                    if (p.Turnover > 0 &&
                        p.OpenBalance > 0 &&
                        Server.Settings.MaxPercentTurnover > 0 &&
                        p.Turnover / p.OpenBalance * 100 >= Server.Settings.MaxPercentTurnover)
                    */
                    if (p.IsMaxPercentTurnoverExceed)
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

                CheckPortfolioMarginCall(p, ruleItems);

                #endregion
            }

            if (ruleItems.Count > 0)
            {
                new CommandMerge
                {
                    Object = Server.PortfolioRules,
                    Data = ruleItems,
                    KeyFields = "TradeCode,RuleType",
                    Fields = "RuleTime",
                }.ExecuteAsync();
            }
        }

        /// <summary>
        /// Проверка портфеля на маржинкол
        /// </summary>
        /// <param name="portfolio"></param>
        /// <param name="rules"></param>
        private static void CheckPortfolioMarginCall(Portfolio portfolio, List<PortfolioRule> rules)
        {
            // не проверяем эти портфели
            if (ContragentTradeCodes.Contains(portfolio.TradeCode))
                return;

            // определяем тип портфеля и вызываем соответсвующий метод определения маржинкола
            switch (portfolio.GroupId)
            {
                case ClientGroup.MMA:
                    CheckMMAPortfolioMarginCall(portfolio, rules);
                    break;
                case ClientGroup.ZAO:
                    CheckZAOPortfolioMarginCall(portfolio, rules);
                    break;
            }
        }

        /// <summary>
        /// Проверка портфеля ЗАО
        /// </summary>
        /// <param name="portfolio"></param>
        /// <param name="rules"></param>
        private static void CheckZAOPortfolioMarginCall(Portfolio portfolio, List<PortfolioRule> rules)
        {
            // •	Для ЗАО: [текущий капитал] < [минимальная маржа] => маржин колл.
            if (Server.Settings.MarginForceClose)
            {
                if (portfolio.Capital < portfolio.MarginMin)
                {
                    // Ставим признак в портфеле Margin Call
                    portfolio.MarginCall = true;
                    return;
                }
            }

            // Снятие Margin Call в случае изменения ситуации
            if (!portfolio.MarginCall) 
                return;

            if (!(portfolio.Capital < portfolio.MarginMin))
            {
                portfolio.MarginCall = false;
            }
        }

        /// <summary>
        /// Проверка портфеля ММА
        /// </summary>
        /// <param name="portfolio"></param>
        /// <param name="rules"></param>
        private static void CheckMMAPortfolioMarginCall(Portfolio portfolio, List<PortfolioRule> rules)
        {
            if (Server.Settings.MarginForceClose && portfolio.UtilizationFact < 0)
            {
                portfolio.MarginCall = true;
                return;
            }

            if (Server.Settings.MarginForceClose && portfolio.UtilizationFact > 0)
            {
                // Превышение % использования капитала при котором должны быть закрыты позиции
                if (Server.Settings.MaxPercentUtilMarginCall > 0 &&
                    portfolio.UtilizationFact >= Server.Settings.MaxPercentUtilMarginCall)
                {
                    // Ставим признак в портфеле Margin Call
                    portfolio.MarginCall = true;
                }
                // Превышение % использования капитала, при которой отправляется предупреждение клиенту
                else if (Server.Settings.NotifyClientMaxPercentUtilExceed &&
                         portfolio.UtilizationFact >= Server.Settings.MaxPercentUtilWarning
                         && Server.Settings.MaxPercentUtilWarning > 0)
                {
                    // Отправка оповещения пользователю и клиенту
                    rules.Add(new PortfolioRule
                    {
                        RuleType = RuleType.MaxPercentUtilWarningExceed,
                        Portfolio = portfolio,
                        RuleTime = Server.Current.ServerTime,
                    });
                }
            }

            // Снятие Margin Call в случае изменения ситуации
            if (portfolio.MarginCall)
            {
                if (!Server.Settings.MarginForceClose ||
                    (Server.Settings.MaxPercentUtilMarginCall > 0 &&
                     portfolio.UtilizationFact < Server.Settings.MaxPercentUtilMarginCall))
                {
                    portfolio.MarginCall = false;
                }
            }
        }

        /// <summary>
        /// Конвертирует в валюту настроек
        /// </summary>
        /// <param name="rates"></param>
        /// <param name="items"></param>
        public static void ApplyRates(IEnumerable<Rate> rates, IEnumerable<Portfolio> items = null)
        {
            foreach (var port in from p in items
                                 join r in rates on new { CurrencyFrom = p.Currency, CurrencyTo = Server.Settings.CurrencyCalc }
                                 equals new { r.CurrencyFrom, r.CurrencyTo } into ps
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

            foreach (var port in from p in items
                                 join r in rates on new { CurrencyFrom = p.Currency, CurrencyTo = Server.Settings.CurrencyDisplay }
                                 equals new { r.CurrencyFrom, r.CurrencyTo } into ps
                                 from r in ps.DefaultIfEmpty()
                                 select new { Portfolio = p, Rate = r })
            {
                if (port.Portfolio.Currency != null && port.Portfolio.Currency.Equals(Server.Settings.CurrencyDisplay))
                {
                    port.Portfolio.TurnoverCurrencyDisplay = port.Portfolio.Turnover;
                    port.Portfolio.CapitalCurrencyDisplay = port.Portfolio.Capital;
                    port.Portfolio.PLCurrencyDisplay = port.Portfolio.PL;
                }
                else if (port.Rate != null)
                {
                    port.Portfolio.TurnoverCurrencyDisplay = port.Portfolio.Turnover * port.Rate.Value;
                    port.Portfolio.CapitalCurrencyDisplay = port.Portfolio.Capital * port.Rate.Value;
                    port.Portfolio.PLCurrencyDisplay = port.Portfolio.PL * port.Rate.Value;
                }
            }
        }        
    }
}