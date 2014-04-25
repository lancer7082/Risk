using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public enum RuleType : int {
        None,
        MaxProfitExceed,                // Превышение прибыли
        MaxPercentProfitExceed,         // Превышение процента прибыли от входящего капитала
        MaxTurnoverExceed,              // Превышение оборота по сделкам
        MaxPercentTurnoverExceed,       // Превышение процента оборота по сделкам от входящего капитала
        MaxPercentUtilMarginCallExceed, // Превышение % использования капитала, при которой закрывается позиция
        MaxPercentUtilWarningExceed,    // Превышение % использования капитала, при которой отправляется уведомление клиенту
        //TransaqPricesNotFound,        // Не изменяются цены из Transaq по выбранным инструментам
    }

    public class PortfolioRule : ICloneable
    {
        /// <summary>
        ///  ИД правила
        //  TODO: Реализовать класс для правил
        public RuleType RuleType { get; set; }

        /// <summary>
        /// Объект, к которому применяются правила 
        /// <remarks> Сейчас Portfolios </remarks>
        /// </summary>
        public Portfolio Portfolio { get; set; }

        /// <summary>
        /// Время первого срабатывания правила 
        /// </summary>
        public DateTime RuleTime { get; set; }

        /// <summary>
        /// Последнее оповещение по правилу
        /// </summary>
        public Alert LastAlert { get; set; }

        /// <summary>
        /// Совершенные способы уведомления о событии
        /// </summary>
        public NotifyType NotifyTypesAccomplished { get; set; }

        public object Clone()
        {
            return new PortfolioRule
            {
                RuleType = this.RuleType,
                Portfolio = this.Portfolio,
                RuleTime = this.RuleTime,
                LastAlert = this.LastAlert,
                NotifyTypesAccomplished = this.NotifyTypesAccomplished,
            };
        }

        public static implicit operator PortfolioRuleInfo(PortfolioRule rule)
        {
            return new PortfolioRuleInfo
            {
                RuleType = rule.RuleType.ToString(),                
                TradeCode = rule.Portfolio != null ? rule.Portfolio.TradeCode : "",
                RuleTime = rule.RuleTime,
                LastAlertText = rule.LastAlert != null ? rule.LastAlert.Text : "",
                LastAlertTime = rule.LastAlert != null ? rule.LastAlert.DateTime : rule.RuleTime,
            };
        }
    }
}
