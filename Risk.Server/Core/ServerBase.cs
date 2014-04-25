using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Risk
{
    /// <summary>
    /// Базовый функционал сервера
    /// </summary>
    public class ServerBase : IDisposable
    {
        private DataBase dataBase;
        protected static readonly Logger log = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource serviceTokenSource = new CancellationTokenSource();
        private static BlockingCollection<CommandServer> serviceQueue = new BlockingCollection<CommandServer>();
        private static ServerBase _server;
        private AddIns _addIns = new AddIns();
        private Dictionary<string, IDataObject> dataObjects = new Dictionary<string, IDataObject>();
        public static bool WithoutProcessQueue = false; // Only for test

        /// <summary>
        /// Имя сервера
        /// </summary>
        public string ServerName { get; private set; }

        /// <summary>
        /// Безопасный режим: не загружаются расширения
        /// </summary>
        public bool SafeMode { get; set; }

        /// <summary>
        /// Подключения
        /// </summary>
        public static readonly Connections Connections = new Connections();

        /// <summary>
        /// Кол-во обрабатывающих системную очередь потоков
        /// </summary>
        public int ProcessCount { get; set; }

        /// <summary>
        /// Период обновления клиентов
        /// </summary>
        public int RefreshTime { get; set; }

        /// <summary>
        /// Модули расшиерения
        /// </summary>
        public AddIns AddIns { get { return _addIns; } }

        /// <summary>
        /// Service Host
        /// </summary>
        public ServiceHost ServiceHost;

        /// <summary>
        /// Текущее время сервера
        /// </summary>
        public DateTime ServerTime
        {
            get { return DateTime.Now; }
        }

        public IEnumerable<IDataObject> DataObjects
        {
            get { return dataObjects.Values; }
        }

        public DataBase DataBase
        {
            get { return dataBase; }
            set { dataBase = value; }
        }

        public ServerBase()
        {
            _server = this;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            ServerName = Environment.MachineName; // TODO: ??? FromConfig
            ProcessCount = 4;    // TODO: !!! Autodetect, FromConfig
            RefreshTime = 2000;  // TODO: !!! FromConfig

            log.Info("{0}", GetVersion());
        }

        public static ServerBase Current
        {
            get
            {
                if (_server == null)
                    throw new Exception("Not found server instance");
                return _server;
            }
        }

        public string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            StringBuilder ssb = new StringBuilder();
            ssb.AppendFormat("{0} v {1}", assembly.GetTitle(), assembly.GetVersion());
            return ssb.ToString();
        }

        public virtual void Configure()
        {
            // Register tables
            Register(Connections);
        }

        public void Start()
        {
            log.Trace("Server starting {0}", ServerName);

            // Start Service Queue
            for (int i = 0; i < ProcessCount; i++)
                Task.Factory.StartNew(ProcessServiceQueue);

            // Start AddIns
            if (!SafeMode)
                _addIns.Start();

            // Start WCF
            if (ServiceHost != null)
            {
                ServiceHost.Opening += (s, e) => log.Trace("Service host Opening");
                ServiceHost.Opened += (s, e) => log.Trace("Service host open");
                ServiceHost.Closing += (s, e) => log.Trace("Service host closing");
                ServiceHost.Closed += (s, e) => log.Trace("Service host closed");
                ServiceHost.Faulted += (s, e) => log.Trace("Service host faulted");
                ServiceHost.UnknownMessageReceived += (s, e) => log.Trace("Service host unknown message received");

                ServiceHost.Open();
                foreach (ServiceEndpoint endpoint in ServiceHost.Description.Endpoints)
                    log.Info(String.Format("Add Endpoint: {0}", endpoint.Address));
            }

            log.Info("Server start at {0}", ServerName);
        }

        public void Stop()
        {
            log.Trace("Server stoppping {0}", ServerName);

            // Stop WCF
            if (ServiceHost != null && ServiceHost.State == CommunicationState.Opened)
            {
                ServiceHost.Abort();
                ServiceHost = null;
            }

            // Stop AddIns
            if (!SafeMode)
                _addIns.Stop();

            // Stop Service Queue
            serviceTokenSource.Cancel();

            log.Info("Server stop at {0}", ServerName);
        }

        public void Dispose()
        {
            Stop();
        }

        internal IDataObject FindDataObject(string objectName)
        {
            if (!String.IsNullOrWhiteSpace(objectName) && dataObjects.ContainsKey(objectName))
                return dataObjects[objectName];
            else
                return null;
        }

        public void Register(IDataObject obj)
        {
            if (String.IsNullOrWhiteSpace(obj.Name))
                throw new Exception("Registered object must have name");

            if (FindDataObject(obj.Name) != null)
                throw new Exception(String.Format("Data object Name '{0}' already exists", obj.Name));

            dataObjects.Add(obj.Name, obj);
            obj.RefreshTime = RefreshTime;
        }

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public IEnumerable<IDataObject> GetDataObjects(Type type)
        {
            return dataObjects.Values.Where(x => IsSubclassOfRawGeneric(type, x.GetType()));
        }

        internal static void ProcessCommand(CommandServer command)
        {
            if (WithoutProcessQueue)
                Execute(command);
            else
                serviceQueue.Add(command);
        }

        private static void Execute(CommandServer command)
        {
            if (command.TaskCompletionSource != null && command.CancellationToken.IsCancellationRequested)
            {
                command.TaskCompletionSource.SetCanceled();
                // log.Trace(String.Format("Command cancel '{0}'", command.ToString()));
            }
            else
                try
                {
                    // log.Trace(String.Format("Command start '{0}'", command.ToString()));
                    command.InternalExecute();
                    if (command.TaskCompletionSource != null && !command.TaskCompletionSource.Task.IsCompleted)
                        command.TaskCompletionSource.SetResult(null);
                    // log.Trace(String.Format("Command stop '{0}'", command.ToString()));
                }
                catch (Exception ex)
                {
                    if (command.TaskCompletionSource != null)
                        command.TaskCompletionSource.SetException(ex);
                    log.TraceException(String.Format("Command error '{0}': {1}", command.ToString(), ex.Message), ex);
                }
            // TODO: ??? Подсчет статистики
            // Interlocked.Add(ref commandProcessed, 1);
        }

        private void ProcessServiceQueue()
        {
            foreach (var command in serviceQueue.GetConsumingEnumerable(serviceTokenSource.Token))
            {
                Execute(command);
            }
        }

        private Dictionary<string, Type> _commands;
        public Dictionary<string, Type> Commands
        {
            get
            {
                if (_commands == null)
                {
                    _commands = new Dictionary<string, Type>();
                    foreach (var type in Assembly.GetExecutingAssembly().GetLoadableTypes())
                    {
                        object[] attrs = type.GetCustomAttributes(typeof(CommandAttribute), true);
                        if (attrs.Length == 1)
                            _commands.Add(((CommandAttribute)attrs[0]).CommandName, type);
                    }
                }
                return _commands;
            }
        }

        public object Execute(Command command, Connection connection = null)
        {
            return CreateCommand(command, connection).Execute();
        }

        public void ExecuteAsync(Command command, Connection connection = null)
        {
            CreateCommand(command, connection).ExecuteAsync();
        }

        public T Execute<T>(Command command, Connection connection = null)
        {
            return (T)Execute(command, connection);
        }

        private CommandServer CreateCommand(Command command, Connection connection = null)
        {
            CommandServer result;

            // Command type
            var commandType = Commands.FirstOrDefault(x => String.Equals(x.Key, command.CommandText, StringComparison.InvariantCultureIgnoreCase)).Value;
            if (commandType == null)
                throw new Exception(String.Format("Invalid command name '{0}'", command.CommandText));

            // Data object type
            IDataObject dataObject = null;
            Type dataObjectType = null;
            var dataObjectName = (string)command.Parameters["ObjectName"];
            if (!String.IsNullOrWhiteSpace(dataObjectName))
            {
                dataObject = FindDataObject(dataObjectName);
                if (dataObject == null)
                    throw new Exception(String.Format("Invalid object name '{0}'", dataObjectName));
                dataObjectType = dataObject.ObjectType;
            }

            if (commandType.IsGenericType)
            {              
                if (dataObject == null)
                    throw new Exception(String.Format("Command '{0}' required ObjectName", Commands.First(x => x.Value == commandType).Key));
                commandType = commandType.MakeGenericType(dataObjectType);
            }

            // Init command
            result = (CommandServer)Activator.CreateInstance(commandType);
            result.Connection = connection;
            result.CorrelationId = command.CorrelationId;
            result.Parameters = command.Parameters;
            result.Object = dataObject;
            result.Data = command.Data;

            return result;
        }
    }
}
