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
        public Alert()
        {
            AlertId = Guid.NewGuid();
        }

        /// <summary>
        /// ИД оповещения
        /// </summary>
        public Guid AlertId { get; set; }

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

        /// <summary>
        /// NotificationId
        /// </summary>
        public long NotificationId { get; set; }

        /// <summary>
        /// Тип сообщения (для передачи на клиента)
        /// </summary>
        public AlertType AlertType { get; set; }

        // Значения для формы сделок по-прежнему берутся по порядку, т.к. колонки генерируются автоматически! 
        // Добавлять новые поля можно только в конец.
        // + В форме клиента необходимо обновить проверку количества колонок и назначить русское название

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
                NotificationId = NotificationId,
                AlertType   = this.AlertType
            };
        }

        public static implicit operator AlertInfo(Alert alert)
        {
            return new AlertInfo
            {
                AlertId = alert.AlertId,
                RuleType = (alert.PortfolioRule != null) ? alert.PortfolioRule.RuleType.ToString() : RuleType.None.ToString(),
                TradeCode = (alert.Portfolio != null) ? alert.Portfolio.TradeCode : "",
                Text = alert.Text,
                DateTime = alert.DateTime,
                NotifyTypes = (alert.NotifyType == NotifyType.None) ? "" : alert.NotifyType.ToString(),
            };
        }
    }
}
