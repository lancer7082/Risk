using System;
using System.Collections.Generic;
using System.Linq;

namespace Risk
{
    /// <summary>
    /// Таблица портфелей, по которым сработали правила
    /// </summary>
    [Table("PortfolioRules", KeyFields = "Portfolio,RuleType")]
    public class PortfolioRules : Table<PortfolioRule, PortfolioRuleInfo>
    {
        public override void TriggerAfter(TriggerCollection<PortfolioRule> items)
        {
            var alerts = new List<Alert>();
            foreach (var ruleItem in from p in items.Updated
                                     where (p.LastAlert == null) || 
                                     // Для проверки рыночных данных интервал не проверяем
                                     //(p.RuleType == RuleType.TransaqPricesNotFound) ||
                                     (
                                        (p.LastAlert != null) && 
                                        (p.RuleTime - p.LastAlert.DateTime >= new TimeSpan(0, 2, 0))
                                     )
                                     select p)
            {
                var alert = new Alert
                { 
                    NotifyType = NotifyType.None,
                    DateTime = Server.Current.ServerTime,
                    PortfolioRule = ruleItem,
                };

                if (ruleItem.Portfolio != null)
                {
                    alert.Portfolio = ruleItem.Portfolio;
                    if (ruleItem.RuleType == RuleType.MaxProfitExceed)
                    {
                        alert.Text = String.Format("Превышен заданный размер прибыли: Прибыль = {0}",
                            Math.Round(ruleItem.Portfolio.PLCurrencyCalc, 2));
                    }
                    else if (ruleItem.RuleType == RuleType.MaxPercentProfitExceed)
                    {
                        alert.Text = String.Format("Превышен заданный % прибыли от входящего капитала: Прибыль = {0}; Входящий капитал = {1}",
                            Math.Round(ruleItem.Portfolio.PL, 2),
                            Math.Round(ruleItem.Portfolio.OpenBalance, 2));
                    }
                    else if (ruleItem.RuleType == RuleType.MaxTurnoverExceed)
                    {
                        alert.Text = String.Format("Превышен заданный оборот по сделкам: Оборот = {0}",
                            Math.Round(ruleItem.Portfolio.TurnoverCurrencyCalc, 2));
                    }
                    else if (ruleItem.RuleType == RuleType.MaxPercentTurnoverExceed)
                    {
                        alert.Text = String.Format("Превышен заданный % оборота по сделкам от входящего капитала: Оборот = {0}; Входящий капитал = {1}",
                            Math.Round(ruleItem.Portfolio.Turnover, 2),
                            Math.Round(ruleItem.Portfolio.OpenBalance, 2));
                    }
                    else if (ruleItem.RuleType == RuleType.MaxPercentUtilMarginCallExceed)
                    {
                        alert.Text = String.Format("Превышен критический уровень % использования капитала (Margin Call): Использование капитала(%) = {0}",
                            Math.Round(ruleItem.Portfolio.UtilizationFact, 2));
                        /*
                        if (Server.Settings.NotifyClientMaxPercentUtilExceed)
                            alert.NotifyType = Server.Settings.NotifyTypeMaxPercentUtilMarginCall;
                        */ 
                    }
                    else if (ruleItem.RuleType == RuleType.MaxPercentUtilWarningExceed)
                    {
                        alert.Text = String.Format("Превышен заданный % использования капитала: Использование капитала(%) = {0}",
                            Math.Round(ruleItem.Portfolio.UtilizationFact, 2));
                        if (Server.Settings.NotifyClientMaxPercentUtilExceed)
                            alert.NotifyType = Server.Settings.NotifyTypeMaxPercentUtilWarning;
                    }
                    else
                    {
                        alert.Text = "Неизвестное правило";
                    };
                }

                alerts.Add(alert);
                ruleItem.LastAlert = alert;
            }

            if (alerts.Count > 0)
                new CommandInsert
                {
                    Object = Server.Alerts,
                    Data = alerts,
                }.ExecuteAsync();
        }
    }
}
