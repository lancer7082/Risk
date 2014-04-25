using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Сообщения (для отправки на клиента)
    /// </summary>
    [Serializable]
    public class AlertInfo : ICloneable
    {
        /// <summary>
        /// ИД оповещения
        /// </summary>
        public int AlertId { get; set; }

        public string RuleType { get; set; }
        public string TradeCode { get; set; }

        /// <summary>
        /// Текст оповещения
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Дата / время оповещения
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Способы оповещения
        /// </summary>
        public string NotifyTypes { get; set; }

        public object Clone()
        {
            return new AlertInfo
            {
                AlertId = this.AlertId,
                RuleType = this.RuleType,
                TradeCode = this.TradeCode,
                Text = this.Text,
                DateTime = this.DateTime,
                NotifyTypes = this.NotifyTypes,
            };
        }
    }
}
