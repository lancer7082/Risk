using System;
using System.Collections.Generic;
using NLog;

namespace Risk
{
    /// <summary>
    /// Таблица подключений
    /// </summary>
    [Table("Connections", KeyFields = "ConnectionId")]
    public class Connections : Table<Connection, ConnectionInfo>
    {
        /// <summary>
        /// Лог
        /// </summary>
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Статический конструктор
        /// </summary>
        static Connections()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        protected internal override void Insert(IEnumerable<Connection> items)
        {
            foreach (var connection in items)
            {
                if (!CheckCredentials(connection.UserName, connection.Password))
                {
                    log.Info(String.Format("Invalid login or password: {0} ({1})", connection.UserName, connection.Address));
                    throw new Exception("Invalid login or password");
                }
                else
                {
                    base.Insert(items);
                    log.Info(String.Format("Connected: {0} ({1}) - {2}", connection.UserName, connection.Address, connection.ConnectionId.ToString()));
                }
            }
        }

        /// <summary>
        /// Проверка учетных данных пользователей
        /// </summary>
        /// <param name="userName">имя</param>
        /// <param name="userPassword">пароль</param>
        /// <returns></returns>
        private bool CheckCredentials(string userName, string userPassword)
        {
#if DEBUG
            if (userName.ToUpper() == "TEST" && userPassword == "Test")
                return true;
#endif
            var userData = ServerBase.Current.ServerConfigurationSection.Authentications[userName.ToUpper()];
            if (userData == null)
                return false;
            return PasswordHash.PasswordHash.ValidatePassword(userPassword, userData.PasswordHash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        protected internal override void Delete(IEnumerable<Connection> items)
        {
            base.Delete(items);
            foreach (var connection in items)
            {
                log.Info(String.Format("Disonnected: {0} ({1}) - {2}", connection.UserName, connection.Address, connection.ConnectionId.ToString()));
            }
        }
    }
}