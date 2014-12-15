using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Тип сообщения
    /// </summary>
    public enum AlertType
    {
        /// <summary>
        /// Простое текстовое сообщение
        /// </summary>
        Text = 0,

        /// <summary>
        /// Сообщение о появлении рассчитанной для закрытия позиции
        /// </summary>
        NewPositionInMarginCall = 1,

        /// <summary>
        /// Сообщение о наличии рассчитанных для закрытия позиций
        /// </summary>
        RemindAboutPositionInMarginCall = 2,
    }

    /// <summary>
    /// Оповещение (для отправки на клиента)
    /// </summary>
    [Serializable]
    public class UserAlert : ICloneable
    {
        /// <summary>
        /// Тип оповещения
        /// </summary>
        public AlertType AlertType { get; set; }

        /// <summary>
        /// Текст оповещения
        /// </summary>
        public string Message { get; set; }
        
        public object Clone()
        {
            return new UserAlert
            {
                AlertType = this.AlertType,
                Message = this.Message
            };
        }
    }
}
