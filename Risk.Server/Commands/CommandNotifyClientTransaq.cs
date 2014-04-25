using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Уведомление клиента в терминале Transaq
    /// </summary>
    [Command("NotifyClientTransaq")]
    public class CommandNotifyClientTransaq : CommandServer
    {
        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Торговый код
        /// </summary>
        public string TradeCode { get; set; }

        /// <summary>
        /// Логин в Transaq
        /// </summary>
        public string Login { get; set; }

        protected internal override void InternalExecute()
        {
            Server.Current.DataBase.NotifyClientTransaq(TradeCode, Login, Message);
        }
    }
}
