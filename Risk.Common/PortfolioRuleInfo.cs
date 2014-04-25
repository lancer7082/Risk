using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class PortfolioRuleInfo : ICloneable
    {
        public string RuleType { get; set; }
        public string TradeCode { get; set; }

        /// <summary>
        /// Время последнего срабатывания правила 
        /// </summary>
        public DateTime RuleTime { get; set; }

        /// <summary>
        /// Время последнего оповещения по правилу
        /// </summary>
        public DateTime LastAlertTime { get; set; }

        public String LastAlertText { get; set; }

        public object Clone()
        {
            return new PortfolioRuleInfo
            {
                RuleType = this.RuleType,
                TradeCode = this.TradeCode,
                RuleTime = this.RuleTime,
                LastAlertTime = this.LastAlertTime,
                LastAlertText = this.LastAlertText,
            };
        }
    }
}
