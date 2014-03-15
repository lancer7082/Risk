using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NLog;

namespace Risk
{
    /// <summary>
    /// Базовый класс подключения к сервреру
    /// </summary>
    public abstract class Connection : IConnection
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private bool Disconnecting = false;

        protected IConnectionCallback _callback;        

        public bool Connected { get; internal set; }
        public Guid Id { get; private set; }
        public string UserName { get; protected set; }
        public string Password { get; protected set; }
        public string Address { get; protected set; }
        public int Port { get; protected set; }
        public DateTime StartTime { get; protected set; }
        public ConcurrentDictionary<string, Notification> Notifications = new ConcurrentDictionary<string, Notification>();

        internal virtual void CheckConnection()
        {
            if (!Connected)
                throw new Exception("The client has not authorized");
        }

        public virtual string Connect(string userName, string password, string connectionId)
        {
            Id = String.IsNullOrWhiteSpace(connectionId) ? Guid.NewGuid() : new Guid(connectionId);
            UserName = userName;
            Password = password;
            (new CommandConnect { Connection = this, Type = CommandType.Create }).Execute();
            Connected = true;
            StartTime = DateTime.Now;
            // FOR DEBUG !!! (new CommandMessage { Connection = this, CommandType = CommandType.Create, Data = "SERVER: Connected" }).Execute();
            // FOR DEBUG !!! (new CommandClient { Connection = this, CommandType = CommandType.Create, Data = new Command { CommandText = "Connected", Data = "TEST1" } }).Execute();
            return Id.ToString();
        }

        public virtual void Disconnect()
        {
            if (!Disconnecting)
            {
                Disconnecting = true;
                // FOR DEBUG !!! (new CommandClient { Connection = this, CommandType = CommandType.Create, Data = new Command { CommandText = "Disconnected", Data = "TEST2" } }).Execute();
                // FOR DEBUG !!! (new CommandMessage { Connection = this, CommandType = CommandType.Create, Data = "SERVER: Disconnected" }).Execute();
                (new CommandConnect { Connection = this, Type = CommandType.Delete }).Execute();
            }
        }

        public object Execute(Command command)
        {
            return command.Execute(this);
        }

        public void SendMessage(string message)
        {
            _callback.ReceiveMessage(message);
        }

        public void SendCommand(Command command)
        {
            _callback.ReceiveCommand(command);
        }

        public void Async(Action action)
        {
            if (Connected)
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        log.InfoException(String.Format("Error connection callback: {0} ({1}) - {2}: {3}", UserName, Address, Id.ToString(), ex.Message), ex);
                    }
                });
        }
    }
}