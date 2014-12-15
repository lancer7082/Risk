using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Risk
{
    /// <summary>
    /// Отправка сообщения клиентам
    /// </summary>
    internal class CommandAlert : CommandServer
    {
        /// <summary>
        /// Тип сообщения
        /// </summary>
        public AlertType AlertType
        {
            get { return (AlertType)Parameters["AlertType"]; }
            set { Parameters["AlertType"] = value; }
        }

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Message
        {
            get { return (string)Parameters["Message"]; }
            set { Parameters["Message"] = value; }
        }

        protected internal override void InternalExecute()
        {
            Command command = new Command
            {
                Data = new UserAlert { Message = this.Message, AlertType = this.AlertType },
            };
            foreach (var connection in Server.Connections)
            {
                connection.SendCommand(command);
            }
        }
    }
}