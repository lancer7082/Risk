using System;
using System.Collections.Generic;
using System.Linq;

namespace Risk
{
    /// <summary>
    /// Таблица портфелей, по которым сработали правила
    /// </summary>
    [Table("PortfolioRules", KeyFields = "TradeCode,RuleType")]
    public class PortfolioRules : Table<PortfolioRule>
    {
        /// <summary>
        /// Временная задержка, используемая для прореживания потока одинаковых алертов.  
        /// </summary>
        /// <remarks> 
        /// Т.е. если за это время придет несколько одинаковых PortfolioRule, то алерт будет создан только один, причем один алерт
        /// в указанный период.
        /// </remarks>
        private readonly TimeSpan _equalAlertsSendingPeriod = new TimeSpan(0, 10, 0);

        /// <summary>
        /// TriggerAfter
        /// </summary>
        /// <param name="items"></param>
        public override void TriggerAfter(TriggerCollection<PortfolioRule> items)
        {
            var alerts = new List<Alert>();

            // выбираем только те правила, по которым либо еще не было создано ни одного алерта
            // либо последний алерт был отправлен больше чем _equalAlertsSendingPeriod назад.
            foreach (var ruleItem in from p in items.Updated
                                     where (p.LastAlert == null) ||
                                     ((p.LastAlert != null) && (p.RuleTime - p.LastAlert.DateTime >= _equalAlertsSendingPeriod))
                                     select p)
            {
                // создаем алерт
                var alert = new Alert
                {
                    NotifyType = NotifyType.None,
                    DateTime = Server.Current.ServerTime,
                    PortfolioRule = ruleItem,
                };

                // заполняем текст алерта в зависимости от правила
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
                        alert.Text = String.Format("Ваши позиции по счету {0} были частично закрыты в связи с недостаточным обеспечением. Использование капитала превысило {1}%.",
                            ruleItem.Portfolio.TradeCode, Server.Settings.MaxPercentUtilMarginCall);
                        if (Server.Settings.MarginForceClose)
                            alert.NotifyType = Server.Settings.NotifyTypeMaxPercentUtilMarginCall;
                    }
                    else if (ruleItem.RuleType == RuleType.MaxPercentUtilWarningExceed)
                    {
                        alert.Text = String.Format("Использование капитала по счету {0} равно {1:.##}%. При превышении использования капитала или равном {2}%, мы будем вынуждены закрыть позиции до {3}% использования капитала.",
                            alert.Portfolio.TradeCode, alert.Portfolio.UtilizationFact, Server.Settings.MaxPercentUtilMarginCall, Server.Settings.PlannedCapitalUtilization);
                        if (Server.Settings.NotifyClientMaxPercentUtilExceed)
                            alert.NotifyType = Server.Settings.NotifyTypeMaxPercentUtilWarning;
                    }
                    else if (ruleItem.RuleType == RuleType.IncorrectGORate)
                    {
                        alert.Text = "Некорректная ставка ГО";
                        alert.NotifyType = NotifyType.None;
                    }
                    else
                    {
                        alert.Text = "Неизвестное правило";
                    }
                }

                alerts.Add(alert);
                ruleItem.LastAlert = alert;  // устанавливаем последний алерт для правила, чтобы потом проверять время отправки
            }

            // отправляем команду добавления созданых алертов в таблицу
            if (alerts.Count > 0)
                new CommandInsert
                {
                    Object = Server.Alerts,
                    Data = alerts,
                }.ExecuteAsync();
        }
    }
}
