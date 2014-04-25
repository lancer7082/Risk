using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NLog;
using System.Collections;

namespace Risk
{
    /// <summary>
    /// Таблица подключений
    /// </summary>
    [Table("Connections", KeyFields = "ConnectionId")]
    public class Connections : Table<Connection, ConnectionInfo>
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        protected internal override void Insert(IEnumerable<Connection> items)
        {
            foreach (var connection in items)
            {
                // TODO: !!! Check login and password
                // TODO: ??? PWDCOMPARE('123', password_hash) = 1
#if DEBUG
                if (connection.UserName != "test" || connection.Password != "Test")
#else
                if ((connection.UserName != "test" || connection.Password != "Test1")
                && (connection.UserName != "moiseev" || connection.Password != "moiseev123"))              
#endif
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