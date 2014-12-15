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
using Risk.Jobs;
using Risk.Configuration;

namespace Risk
{
    /// <summary>
    /// Базовый функционал сервера
    /// </summary>
    public class ServerBase : IDisposable
    {
        /// <summary>
        /// Лог
        /// </summary>
        protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// _serviceTokenSource
        /// </summary>
        private readonly CancellationTokenSource _serviceTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Очередь сервера
        /// </summary>
        private static readonly BlockingCollection<CommandServer> ServiceQueue = new BlockingCollection<CommandServer>();

        /// <summary>
        /// Инстанс сервера
        /// </summary>
        private static ServerBase _server;

        /// <summary>
        /// Расширения
        /// </summary>
        private readonly AddIns _addIns = new AddIns();

        /// <summary>
        /// Дата-объекты сервера
        /// </summary>
        private readonly Dictionary<string, IDataObject> _dataObjects = new Dictionary<string, IDataObject>();

        /// <summary>
        /// Не использовать очередь сервера при обработке команд
        /// </summary>
        public static bool WithoutProcessQueue = false; // Only for test

        /// <summary>
        /// Менеджер джобов
        /// </summary>
        public readonly JobManager JobManager = new JobManager();

        /// <summary>
        /// Имя сервера
        /// </summary>
        public string ServerName { get; set; }

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
        /// Секция настроек сервера
        /// </summary>
        public RiskServerSection ServerConfigurationSection;

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

        /// <summary>
        /// Дата-объекты сервера
        /// </summary>
        public IEnumerable<IDataObject> DataObjects
        {
            get { return _dataObjects.Values; }
        }

        /// <summary>
        /// Объект для работы с БД
        /// </summary>
        public DataBase DataBase { get; set; }

        /// <summary>
        /// Команды
        /// </summary>
        private Dictionary<string, Type> _commands;

        /// <summary>
        /// Лок для инициализации команд
        /// </summary>
        private readonly object _commandsLocker = new object();

        /// <summary>
        /// Команды
        /// </summary>
        public Dictionary<string, Type> Commands
        {
            get
            {
                if (_commands == null)
                {
                    lock (_commandsLocker)
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
                    }
                }
                return _commands;
            }
        }

        /// <summary>
        /// Инстанс сервера
        /// </summary>
        public static ServerBase Current
        {
            get
            {
                if (_server == null)
                    throw new Exception("Not found server instance");
                return _server;
            }
        }

        /// <summary>
        /// Версия сервера
        /// </summary>
        public string Version
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var ssb = new StringBuilder();
                ssb.AppendFormat("{0} v {1}", assembly.GetTitle(), assembly.GetVersion());
                return ssb.ToString();
            }
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public ServerBase()
        {
            _server = this;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            Log.Info("{0}", Version);
        }

        /// <summary>
        /// Выполнить конфигурирование сервера
        /// </summary>
        public virtual void Configure()
        {
            // Register tables
            Register(Connections);
        }

        /// <summary>
        /// Запуск сервера
        /// </summary>
        public void Start()
        {
            Log.Trace("Server starting {0}", ServerName);

            // Start Service Queue
            for (int i = 0; i < ProcessCount; i++)
                Task.Factory.StartNew(ProcessServiceQueue);

            // Start AddIns
            if (!SafeMode)
                _addIns.StartAll();

            // Start WCF
            OpenWCFConnection();
            
            Log.Info("Server start at {0}", ServerName);
        }

        /// <summary>
        /// Инициализация WCFConnection
        /// </summary>
        protected virtual void InitWCFConnection()
        {
        }

        /// <summary>
        /// Открыть WCF соединение
        /// </summary>
        private void OpenWCFConnection()
        {
            if (ServiceHost == null)
                return;

            ServiceHost.Opening += (s, e) => Log.Trace("Service host Opening");
            ServiceHost.Opened += (s, e) => Log.Trace("Service host open");
            ServiceHost.Closing += (s, e) => Log.Trace("Service host closing");
            ServiceHost.Closed += (s, e) => Log.Trace("Service host closed");
            ServiceHost.Faulted += ServiceHostFaulted;
            ServiceHost.UnknownMessageReceived += (s, e) => Log.Trace("Service host unknown message received");

            try
            {
                ServiceHost.Open();
            }
            catch (Exception e)
            {
                Log.ErrorException("ServiceHost.Open exception " + e.Message, e);
                RestartServiceByReason("Can't open service host");
            }

            foreach (ServiceEndpoint endpoint in ServiceHost.Description.Endpoints)
                Log.Info(String.Format("Add Endpoint: {0}", endpoint.Address));
        }

        /// <summary>
        /// Обработчик события Fault ServiceHostа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ServiceHostFaulted(object sender, EventArgs e)
        {
            Log.Error("Service host faulted");
            RestartServiceByReason("Service host faulted");
            //return;
            //ServiceHost.Abort();
            //ServiceHost = null;
            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            //InitWCFConnection();
            //OpenWCFConnection();
        }

        /// <summary>
        /// Перезапуск сервиса
        /// </summary>
        /// <param name="reason"></param>
        private void RestartServiceByReason(string reason)
        {
            return; // Нужно убрать рестарт сервиса при недоступности transaq из-за мониторинга
            int waitTime;

            // c 00:00 до 02:30 задержка перезапуска 5 минут, в остальное время - 30 секунд
            if (ServerTime.TimeOfDay.TotalMinutes >= 30 && ServerTime.TimeOfDay.TotalMinutes < (60 * 2 + 30))
                waitTime = 1000 * 60 * 5;
            else
                waitTime = 1000 * 30;

            Log.Info(@"Server will be restarted by reason ""{0}"" after {1} seconds", reason, waitTime / 1000);
            Thread.Sleep(waitTime);

            // генерация исключения, которое завалит сервис и тем самым вынудит его перезапуститься.
            //throw new Exception("Server restarting by reason: " + reason);
            Environment.Exit(54);
        }

        /// <summary>
        /// Остановка сервера
        /// </summary>
        public void Stop()
        {
            Log.Trace("Server stoppping {0}", ServerName);

            // Stop WCF
            if (ServiceHost != null && ServiceHost.State == CommunicationState.Opened)
            {
                ServiceHost.Abort();
                ServiceHost = null;
            }

            // Stop AddIns
            if (!SafeMode)
                _addIns.StopAll();

            JobManager.StopAndRemoveAllJobs();

            // Stop Service Queue
            _serviceTokenSource.Cancel();

            Log.Info("Server stop at {0}", ServerName);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Поиск дата-объекта сервера по имени
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        internal IDataObject FindDataObject(string objectName)
        {
            if (!String.IsNullOrWhiteSpace(objectName) && _dataObjects.ContainsKey(objectName))
                return _dataObjects[objectName];
            else
                return null;
        }

        /// <summary>
        /// Поиск дата-объекта сервера по имени
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public IDataObject DataObject(string objectName)
        {
            var result = FindDataObject(objectName);
            if (result == null)
                throw new Exception(String.Format("Object name '{0}' not found", objectName));
            return result;
        }

        /// <summary>
        /// Регистрация дата-объекта
        /// </summary>
        /// <param name="obj"></param>
        public void Register(IDataObject obj)
        {
            if (String.IsNullOrWhiteSpace(obj.Name))
                throw new Exception("Registered object must have name");

            if (FindDataObject(obj.Name) != null)
                throw new Exception(String.Format("Data object Name '{0}' already exists", obj.Name));

            _dataObjects.Add(obj.Name, obj);
            obj.RefreshTime = RefreshTime;
        }

        /// <summary>
        /// Определяет является ли тип обобщенным
        /// </summary>
        /// <param name="generic"></param>
        /// <param name="toCheck"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Влвзращает дата-объекты по типу
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<IDataObject> GetDataObjects(Type type)
        {
            return _dataObjects.Values.Where(x => IsSubclassOfRawGeneric(type, x.GetType()));
        }

        /// <summary>
        /// Обработка команды
        /// </summary>
        /// <param name="command"></param>
        internal static void ProcessCommand(CommandServer command)
        {
            if (WithoutProcessQueue)
                Execute(command);
            else
                ServiceQueue.Add(command);
        }

        /// <summary>
        /// Выполнение команды
        /// </summary>
        /// <param name="command"></param>
        private static void Execute(CommandServer command)
        {
            if (command.TaskCompletionSource != null && command.CancellationToken.IsCancellationRequested)
            {
                command.TaskCompletionSource.SetCanceled();
            }
            else
                try
                {
                    command.InternalExecute();
                    if (command.TaskCompletionSource != null && !command.TaskCompletionSource.Task.IsCompleted)
                        command.TaskCompletionSource.SetResult(null);
                }
                catch (Exception ex)
                {
                    if (command.TaskCompletionSource != null)
                        command.TaskCompletionSource.SetException(ex);
                    Log.ErrorException(String.Format("Command error '{0}': {1}", command, ex.Message), ex);
                }
            // TODO: ??? Подсчет статистики
            // Interlocked.Add(ref commandProcessed, 1);
        }

        /// <summary>
        /// Обоаботка очереди сервера
        /// </summary>
        private void ProcessServiceQueue()
        {
            foreach (var command in ServiceQueue.GetConsumingEnumerable(_serviceTokenSource.Token))
            {
                Execute(command);
            }
        }

        /// <summary>
        /// Выполнение команды
        /// </summary>
        /// <param name="command"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public object Execute(Command command, Connection connection = null)
        {
            return CreateServerCommand(command, connection).Execute();
        }

        /// <summary>
        /// Выполнение команды асинхронное
        /// </summary>
        /// <param name="command"></param>
        /// <param name="connection"></param>
        public void ExecuteAsync(Command command, Connection connection = null)
        {
            CreateServerCommand(command, connection).ExecuteAsync();
        }

        /// <summary>
        /// Выполнение команды
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public T Execute<T>(Command command, Connection connection = null)
        {
            return (T)Execute(command, connection);
        }

        /// <summary>
        /// Создание команды сервера
        /// </summary>
        /// <param name="command"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        internal CommandServer CreateServerCommand(Command command, Connection connection = null)
        {
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
            var result = (CommandServer)Activator.CreateInstance(commandType);
            result.Connection = connection;
            result.CorrelationId = command.CorrelationId;
            result.Parameters = command.Parameters;
            result.Object = dataObject;
            result.Data = command.Data;

            return result;
        }
    }
}
