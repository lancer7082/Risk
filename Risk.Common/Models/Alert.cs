using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Оповещения
    /// </summary>
    [Serializable]
    public class Alert : ICloneable
    {
        /// <summary>
        /// ИД оповещения
        /// </summary>
        //[DataMember]
        public int AlertId { get; set; }

        /// <summary>
        /// Портфолио
        /// </summary>
        public Portfolio Portfolio { get; set; }

        /// <summary>
        /// Правило, на основании которого создано оповещение
        /// </summary>
        public PortfolioRule PortfolioRule { get; set; }

        /// <summary>
        /// Способы оповещения клиента
        /// </summary>
        public NotifyType NotifyType { get; set; }
        
        /// <summary>
        /// Текст оповещения
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Дата / время оповещения
        /// </summary>
        public DateTime DateTime { get; set; }

        public object Clone()
        {
            return new Alert
            {
                AlertId     = this.AlertId,
                Portfolio   = this.Portfolio,
                DateTime    = this.DateTime,
                Text        = this.Text,
                NotifyType  = this.NotifyType,
                PortfolioRule = this.PortfolioRule,
            };
        }

        public static implicit operator AlertInfo(Alert alert)
        {
            return new AlertInfo
            {
                AlertId     = alert.AlertId,
                RuleType    = (alert.PortfolioRule != null) ? alert.PortfolioRule.RuleType.ToString() : RuleType.None.ToString(),
                TradeCode   = (alert.Portfolio != null) ? alert.Portfolio.TradeCode : "",
                Text        = alert.Text,
                DateTime    = alert.DateTime,
                NotifyTypes = (alert.NotifyType == NotifyType.None) ? "" : alert.NotifyType.ToString(),
            };
        }
    }
}
