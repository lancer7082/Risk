using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NLog;

namespace Risk
{
    /// <summary>
    /// Подписка клиента на обновление данных
    /// </summary>
    public class Notification
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public Connection Connection { get; private set; }
        public string CorrelationId { get; set; }
        public IDataObject DataObject { get; set; }
        public Delegate Predicate { get; set; }

        public Notification(Connection connection)
        {
            this.Connection = connection;
        }

        public void Notify(NotificationType notificationType, object data)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Connection.SendCommand(new Command
                    {
                        CommandText = notificationType.ToString(),
                        CorrelationId = CorrelationId,
                        Data = data,
                        Parameters = { new Parameter("ObjectName", DataObject.Name) }
                    });
                }
                catch (Exception ex)
                {
                    log.Info(String.Format("Error notifiction {0}: {1} ({2}) - {3}: {4}", DataObject.Name, Connection.UserName, Connection.Address, Connection.ConnectionId.ToString(), ex.Message));
                }
            });
        }
    }
}