using System;
using System.Linq;

namespace Risk
{    
    /// <summary>
    /// Отправка сообщения клиентам
    /// </summary>
    internal class CommandMessage : CommandServer
    {
        /// <summary>
        /// Тип сообщения
        /// </summary>
        public MessageType MessageType 
        {
            get { return (MessageType)Parameters["MessageType"]; }
            set { Parameters["MessageType"] = value; }
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
            foreach (var connection in Server.Connections)
            {
                connection.SendMessage(Message, MessageType);
            }
        }
    }
}