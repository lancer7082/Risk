using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NLog;

namespace Risk
{
    /// <summary>
    /// Базовый класс подключения к сервреру
    /// </summary>
    public class Connection : IConnection, ICloneable
    {
        /// <summary>
        /// Группы пользователей
        /// </summary>
        private enum UserGroup
        {
            /// <summary>
            /// Admin
            /// </summary>
            Admin = 1,

            /// <summary>
            /// Dealer
            /// </summary>
            Dealer = 2
        }

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        protected IConnectionCallback _callback;

        public Guid ConnectionId { get; private set; }
        public string UserName { get; protected set; }
        public string Address { get; protected set; }
        public int Port { get; protected set; }
        public DateTime StartTime { get; protected set; }
        public bool Connected { get; internal set; }
        public string Password { get; protected set; }
        public bool IsTrace { get; protected set; }
        public ConcurrentDictionary<string, Notification> Notifications = new ConcurrentDictionary<string, Notification>();

        internal virtual void CheckConnection()
        {
            if (!Connected)
                throw new Exception("The client has not authorized");
        }

        public virtual ServerConnectionInfo Connect(string userName, string password, string options = null)
        {
            ConnectionId = Guid.NewGuid();
            UserName = userName;
            Password = password;
            new CommandInsert { Connection = this, Object = Server.Connections, Data = new Connection[] { this } }.Execute();
            StartTime = DateTime.Now;

            if (!String.IsNullOrWhiteSpace(options))
            {
                var csb = new ConnectionStringBuilder(options);
                IsTrace = csb.Trace;
            }

            Connected = true;
            Trace("Connected in trace mode");

            return new ServerConnectionInfo
                {
                    ConnectionId = ConnectionId.ToString(),
                    ServerName = Server.Current.ServerName,
                    ServerVersion = Server.Current.Version,
                    UserName = UserName,
                };
        }

        public virtual void Disconnect()
        {
            if (Connected)
            {
                Trace("Disconnected in trace mode");
                Connected = false;

                new CommandDelete { Connection = this, Object = Server.Connections, Data = new Connection[] { this } }.Execute();
            }
        }

        /// <summary>
        /// Проверка на принадлежность пользователя к группе админов
        /// </summary>
        /// <returns></returns>
        public bool IsAdminUser()
        {
            // проверяем что текущий пользователь принадлежить админской группе
            var userGroup = ServerBase.Current.ServerConfigurationSection.UsersToGroups[UserName.ToUpper()];
            return userGroup != null && userGroup.GroupId == (int)UserGroup.Admin;
        }

        /// <summary>
        /// Проверка на принадлежность пользователя к группе дилеров
        /// </summary>
        /// <returns></returns>
        public bool IsDealerUser()
        {
            // проверяем что текущий пользователь принадлежить группе дилеров
            var userGroup = ServerBase.Current.ServerConfigurationSection.UsersToGroups[UserName.ToUpper()];
            return userGroup != null && (userGroup.GroupId == (int)UserGroup.Dealer || userGroup.GroupId == (int)UserGroup.Admin);
        }

        /// <summary>
        /// Проверка на принадлежность пользователя к группе админов
        /// </summary>
        /// <returns></returns>
        public void CheckAdminUser()
        {
            if (!IsAdminUser())
                throw new Exception("Admin privileges required");
        }

        /// <summary>
        /// Проверка на принадлежность пользователя к группе дилеров
        /// </summary>
        /// <returns></returns>
        public void CheckDealerUser()
        {
            if (!IsDealerUser())
                throw new Exception("Dealer privileges required");
        }

        public void Trace(string message, params object[] args)
        {
            if (IsTrace)
                SendMessage(String.Format("{1:HH:mm:ss.fff} : {0}", String.Format(message, args), DateTime.Now), MessageType.Trace);
        }

        public virtual CommandResult Execute(Command command)
        {
            Trace("Start command '{0}' on server", command);
            log.Trace("Start Execute command {0}", command);
            var serverCommand = Server.Current.CreateServerCommand(command, this);
            var resultData = serverCommand.Execute();
            log.Trace("Stop Execute command {0}", command);
            Trace("Stop command '{0}' on server", command);
            return new CommandResult { Data = resultData, FieldsInfo = serverCommand.FieldsInfo };
        }

        public void SendMessage(string message, MessageType messageType)
        {
            log.Trace("Start send message {0}: {1}", messageType, message);
            Async(() => _callback.ReceiveMessage(message, messageType));
            log.Trace("Stop send message {0}: {1}", messageType, message);
        }

        public void SendCommand(Command command)
        {
            log.Trace("Start send command {0}", command);
            Async(() => _callback.ReceiveCommand(command));
            log.Trace("Stop send command {0}", command);
        }

        public void Async(Action action)
        {
            if (Connected)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (Connected) action();
                    }
                    catch (Exception ex)
                    {
                        log.InfoException(String.Format("Error connection callback: {0} ({1}) - {2}: {3}", UserName, Address, ConnectionId.ToString(), ex.Message), ex);
                    }
                });
        }

        public static implicit operator ConnectionInfo(Connection connection)
        {
            return new ConnectionInfo
            {
                Address = connection.Address,
                ConnectionId = connection.ConnectionId.ToString(),
                Port = connection.Port,
                StartTime = connection.StartTime,
                UserName = connection.UserName
            };
        }

        public object Clone()
        {
            return this;
        }
    }
}