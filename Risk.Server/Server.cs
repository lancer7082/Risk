using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Risk
{
    public class Server : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private CancellationTokenSource serviceTokenSource = new CancellationTokenSource();
        private static BlockingCollection<CommandServer> serviceQueue = new BlockingCollection<CommandServer>();
        private AddIns _addIns = new AddIns();

        private byte firmId;
        private DataBase dataBase;

        private List<Client> clients;
        private List<Portfolio> portfolios;
        private List<Rate> rates;
        private List<Trade> trades;

        private Timer timer;
        private Timer timerMoneyInOutDay;
        private Timer timerRates;

        public int ProcessCount { get; set; }
        public ServiceHost ServiceHost;
        public AddIns AddIns { get { return _addIns; } }

        public List<Portfolio> Portfolios
        {
            get { return portfolios; }
        }

        private object o = new Object();

        public Server()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            log.Info("{0}", GetVersion());

            ProcessCount = 4; // TODO: !!! Autodetect

            clients = new List<Client>();
            portfolios = new List<Portfolio>();
            rates = new List<Rate>();
            trades = new List<Trade>();
        }

        public string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            StringBuilder ssb = new StringBuilder();
            ssb.AppendFormat("{0} v {1}", assembly.GetTitle(), assembly.GetVersion());
            return ssb.ToString();
        }

        public void Configure()
        {
            // Load from DataBase
            var css = ConfigurationManager.ConnectionStrings["Risk"];
            if (css == null)
                throw new Exception("Not found connection string 'Risk' in config");
            var firmIdConfig = ConfigurationManager.AppSettings["FirmId"];
            if (firmIdConfig == null)
                throw new Exception("Not found AppSettings 'FirmId' in config");
            firmId = byte.Parse(firmIdConfig);
            dataBase = new DataBase(css.ConnectionString, firmId);
            
            // TODO: !!! Обновлять данные по таймеру
            log.Info("Start load from Database (FirmId = {0}): {1}", firmId, css.ConnectionString);
            clients = new List<Client>(dataBase.GetClients());
            portfolios = new List<Portfolio>(dataBase.GetPortfolios());
            //rates = new List<Rate>(dataBase.GetRates(DateTime.Now));

            log.Info("Stop load from Database");

            // Получение курсов
            timerRates = new Timer(c =>            
            {
                lock (o)
                try
                {
                    dataBase.CheckConnection();
                    var list = dataBase.GetRates(DateTime.Now);
                    list = list.ToList();
                    foreach (var item in list)
                    {
                        var rate = rates.FirstOrDefault(r => ( (r.CurrencyFrom == item.CurrencyFrom) && (r.CurrencyTo == item.CurrencyTo) ));
                        if (rate == null)
                        {
                            rates.Add(item);
                        }
                        else
                        {
                            rate.Value = item.Value;
                            rate.Date = item.Date;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.InfoException(String.Format("Database error: {0}", ex.Message), ex);
                }
            }, null, 0, 120 * 1000);

            // Start timer update portfolios from SQL
            timerMoneyInOutDay = new Timer(c =>            
            {
                lock (o)
                try
                {
                    dataBase.CheckConnection();
                    var moneyInOutDay = dataBase.GetMoneyInOutDay();
                    moneyInOutDay = moneyInOutDay.ToList();
                    foreach (var portfolio in portfolios)
                    {
                        var account = moneyInOutDay.FirstOrDefault(p => p.AccountId == portfolio.AccountId);
                        if (account != null)
                        {
                            portfolio.MoneyInDay = account.MoneyInDay;
                            portfolio.MoneyOutDay = account.MoneyOutDay;
                        }
                        else
                        {
                            portfolio.MoneyInDay = 0;
                            portfolio.MoneyOutDay = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.InfoException(String.Format("Database error: {0}", ex.Message), ex);
                }
            }, null, 0, 60 * 1000);

            // Start timer command
            timer = new Timer(c => (new CommandTimer()).ExecuteAsync(), null, 0, 1000);

            // Configure AddIns
            AddIns.Register("Risk.Transaq.TransaqAddIn, Risk.Transaq", 0);

            // Configure WCF
            ServiceHost = new ServiceHost(typeof(ConnectionWCF) /*, new Uri("http://localhost:8001/Risk") */);
            ServiceDescription serviceDesciption = ServiceHost.Description;
            if (serviceDesciption.Endpoints.Count == 0)
            {
                var NetTCPUri = new Uri("net.tcp://localhost:26455/Risk");
                var binding = new NetTcpBinding(SecurityMode.None);
                ServiceHost.AddServiceEndpoint(typeof(IConnection), binding, NetTCPUri.ToString());
            }
            //foreach (ServiceEndpoint endpoint in serviceDesciption.Endpoints)
            //{
            //    endpoint.Behaviors.Add(new ConnectionEndpointBehaviour(this));
            //}

            // Service description
            /*
            ServiceMetadataBehavior smb = serviceHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (smb == null)
            {
                smb = new ServiceMetadataBehavior();
                serviceHost.Description.Behaviors.Add(smb);
            }
            smb.HttpGetEnabled = true;
            serviceHost.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
            */
        }

        public void Start()
        {
            log.Trace("Server starting");
            // Start Service Queue
            for (int i = 0; i < ProcessCount; i++)
                Task.Factory.StartNew(ProcessServiceQueue);           

            // Start AddIns
            _addIns.Start();

            // Start WCF
            if (ServiceHost != null)
            {
                ServiceHost.Open();
                foreach (ServiceEndpoint endpoint in ServiceHost.Description.Endpoints)
                    log.Info(String.Format("Add Endpoint: {0}", endpoint.Address));
            }
            log.Info("Server start");
        }

        public void Stop()
        {
            log.Trace("Server stoppping");
            // Stop WCF
            if (ServiceHost != null && ServiceHost.State == CommunicationState.Opened)
                ServiceHost.Close();

            // Stop AddIns
            _addIns.Stop();

            // Stop Service Queue
            serviceTokenSource.Cancel();
            log.Info("Server stop");
        }

        public void Dispose()
        {
            Stop();
        }

        internal static void ProcessCommand(CommandServer command)
        {
            serviceQueue.Add(command);
        }

        private object GetData(Type dataType)
        {
            if (dataType == typeof(IEnumerable<Portfolio>))
                return portfolios.ToArray();
            else
                throw new Exception(String.Format("Not supported data type '{0}'", dataType.Name));
        }

        private IEnumerable GetData(DataObject dataObject)
        {
            switch (dataObject)
            {
                case DataObject.Clients:
                    return clients.ToArray();
                case DataObject.Portfolios:
                    return portfolios.ToArray();
                case DataObject.Connections:
                    return (from c in connections.Values
                            select new ConnectionInfo { UserName = c.UserName, Address = c.Address, Port = c.Port, StartTime = c.StartTime, Id = c.Id.ToString() }).ToArray();
                default:
                    throw new Exception(String.Format("Not supported data type '{0}'", dataObject.ToString()));
            }
        }

        private object GetData(string commandText)
        {
            // Check commandText
            if (String.IsNullOrWhiteSpace(commandText))
                throw new Exception("Command Text is empty");

            // System property
            else if (commandText.StartsWith("@@"))
            {
                switch (commandText.Substring(2).ToLowerInvariant())
                {
                    case "version":
                        return GetVersion();
                    default:
                        throw new Exception(String.Format("Unknown system proprty '{0}'", commandText.Substring(2)));
                }
            }

            // TODO: ??? Parse commandText

            // Get Data Object
            else
                return GetData(GetDataObject(commandText));
        }

        private DataObject GetDataObject(string objectName)
        {
            DataObject _object;
            if (!Enum.TryParse<DataObject>(objectName, out _object))
                throw new Exception(String.Format("Unknown data object '{0}'", objectName));
            return _object;
        }

        private static void UpdateInstance(object source, object destination, IEnumerable<string> fields = null)
        {
            var properties = from p in destination.GetType().GetProperties()
                             where fields == null || fields.Contains(p.Name)
                             select p;
            //var properties = destination.GetType().GetProperties();                
            foreach (PropertyInfo prop in properties)
            {
                var value = prop.GetValue(source, null);
                prop.SetValue(destination, value, null);
            }
        }

        private void UpdateData(CommandServer cmd)
        {
            var dataObject = GetDataObject(cmd.Text);
            // Счета
            if (dataObject == DataObject.Portfolios)
            {
                var data = (IEnumerable<Portfolio>)cmd.Data;
                foreach (var dataPortfolio in data)
                {
                    var portfolio = portfolios.FirstOrDefault(p => p.AccountId == dataPortfolio.AccountId);
                    if (portfolio == null)
                    {
                        portfolio = new Portfolio();
                        UpdateInstance(dataPortfolio, portfolio, cmd.Fields);
                        portfolios.Add(portfolio);
                    }
                    else
                        UpdateInstance(dataPortfolio, portfolio, cmd.Fields);
                }
            }
            // Сделки
            else if (dataObject == DataObject.Trades)
            {
                //Заполнение списка сделок
                var tradesForUpdate = (IEnumerable<Trade>)cmd.Data;
                //var accounts = new List<string>();
                foreach (var dataTrade in tradesForUpdate)
                {
                    Trade trade = trades.FirstOrDefault(t => (t.TradeNo == dataTrade.TradeNo));
                    if (trade == null)
                    {
                        trade = new Trade();
                        UpdateInstance(dataTrade, trade, cmd.Fields);
                        trades.Add(trade);
                        //if (!accounts.Contains(trade.TradeCode))
                        //    accounts.Add(trade.TradeCode);
                    }
                    else
                        UpdateInstance(dataTrade, trade, cmd.Fields);
                }

                var tradesForPortfolio = from t in tradesForUpdate
                                         group t by t.TradeCode into g
                                         select new { TradeCode = g.Key, Turnover = g.Sum(t => t.Value) };

                foreach (var p in from p in portfolios
                                  join t in tradesForPortfolio on p.TradeCode equals t.TradeCode
                                  select new { Portfolio = p, Turnover = t.Turnover})
                {
                    p.Portfolio.Turnover = (decimal)p.Turnover;
                }

                // new Command { Type = CommandType.Update, Text = "Portfolios", Data = portfolios, Fields = new string[] { "" } };

                //var portfoliosForUpdate = (from p in portfolios
                //                           join t in trades on p.TradeCode equals t.TradeCode
                //                           select new { p = p, t = t }
                //                          ).GroupBy(p => p.p.AccountId)
                //                           .Select( p => new { p.First().p, p.Sum(t => t.t.Value) });


                                           //group tp by p.AccountId into g
                                           

                                           //join r in rates on g.First().First() b.First()..Currency equals r.CurrencyFrom
                                           //select new { p = g.First(), turnover = g.Sum(g => g.) }).ToArray();

                //new Command { Type = CommandType.Update, Text = "Portfolios", Data = portfolios, Fields = new string[] {""} }

                // Пересчет оборотов по списку счетов (по которым пришли сделки)
                //foreach(var p in portfoliosForUpdate)
                //{
                //    var turnover = trades.Where(t => t.TradeCode == p.TradeCode).Sum(t => t.Value);
                //    portfolio.Turnover = (decimal)turnover;
                //    //конвертация в USD
                //    if (!portfolio.Currency.Equals("USD"))
                //    {
                //        var rate = rates.FirstOrDefault(r => (r.CurrencyTo.Equals("USD") && r.CurrencyFrom.Equals(portfolio.Currency)));
                //        if (rate != null)
                //            portfolio.Turnover *= rate.Value;
                //        else
                //            // Если курс не найден, то обнуляем оборот 
                //            portfolio.Turnover = 0;
                //    }
                //}
            }
            //Курсы
            else if (dataObject == DataObject.Rates)
            {
                var data = (IEnumerable<Rate>)cmd.Data;
                foreach (var dataRate in data)
                {
                    var rate = rates.FirstOrDefault(r => (r.CurrencyFrom.Equals(dataRate.CurrencyFrom) && r.CurrencyTo.Equals(dataRate.CurrencyTo)));
                    if (rate == null)
                    {
                        rate = new Rate();
                        UpdateInstance(dataRate, rate);
                        rates.Add(rate);
                    }
                    else
                        UpdateInstance(dataRate, rate);
                }
            }
            else
                throw new Exception(String.Format("Not supported update data '{0}'", dataObject));
        }

        private TimeSpan dateNow = DateTime.Now.TimeOfDay;

        private static readonly ConcurrentDictionary<Guid, Connection> connections = new ConcurrentDictionary<Guid, Connection>();

        private Connection Connect(Connection connection)
        {
            // TODO: !!! Check login and password
            if (connection.UserName != "test" || connection.Password != "Test")
            {
                log.Info(String.Format("Invalid login or password: {0} ({1})", connection.UserName, connection.Address));
                throw new Exception("Invalid login or password");
            }
            else
            {
                var newConnection = connections.AddOrUpdate(connection.Id, connection, (key, oldValue) => oldValue = connection);
                if (newConnection == connection)
                    log.Info(String.Format("Connected: {0} ({1}) - {2}", connection.UserName, connection.Address, connection.Id.ToString()));
                else
                    log.Info(String.Format("Restored connection: {0} ({1}) - {2}", connection.UserName, connection.Address, connection.Id.ToString()));
                return newConnection;
            }
        }

        private void Disconnect(Connection connection)
        {
            Connection oldConnection;
            if (connections.TryRemove(connection.Id, out oldConnection))
                log.Info(String.Format("Disonnected: {0} ({1}) - {2}", connection.UserName, connection.Address, connection.Id.ToString()));
        }

        private void SubscriberAction(DataObject dataObject, Action<Connection, Notification> action)
        {
            foreach (var notify in from c in connections.Values
                                   // where c.Connected && c.Notifications.ContainsKey(dataObject)
                                   from n in c.Notifications.Values
                                   where n.DataObject == dataObject
                                   select new { Connection = c, Notification = n })
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        action(notify.Connection, notify.Notification);
                    }
                    catch (Exception ex)
                    {
                        log.Info(String.Format("Error notifiction {0}: {1} ({2}) - {3}: {4}", dataObject, notify.Connection.UserName, notify.Connection.Address, notify.Connection.Id.ToString(), ex.Message));
                    }
                });
        }

        private void NotifyUpdateAll(DataObject dataObject, object data)
        {
            SubscriberAction(dataObject, (c, n) =>
            {
                c.SendCommand(new Command
                {
                    Type = CommandType.Update,
                    Text = dataObject.ToString(),
                    CorrelationId = n.CorrelationId,
                    Data = data
                });
            });
        }

        private void Timer()
        {
            dateNow = DateTime.Now.TimeOfDay;

            NotifyUpdateAll(DataObject.Connections, GetData(DataObject.Connections));

            // Update clients
            foreach (var client in clients)
            {
                client.TimeUpdate = dateNow;
                client.Updated = true;
            }

            // Notifications clients
            var updateList = from c in clients
                             where c.Updated
                             select c;
            NotifyUpdateAll(DataObject.Clients, updateList.ToArray());

            // Update portfolios
            foreach (var portfolio in portfolios)
            {
                portfolio.UpdateTime = dateNow;
            }

            // Notifications portfolios
            var updatePortfolios = from p in portfolios
                                   select p;
            NotifyUpdateAll(DataObject.Portfolios, updatePortfolios.ToArray());
        }

        private void ProcessServiceQueue(CommandServer command)
        {
            if (command is CommandTimer)
            {
                Timer();
            }
            else if (command is CommandConnect)
            {
                if (command.Type == CommandType.Create)
                    Connect(command.Connection);
                else if (command.Type == CommandType.Delete)
                    Disconnect(command.Connection);
            }
            else if (command is CommandMessage)
            {
                if (command.Type == CommandType.Create)
                    command.Connection.Async( () => command.Connection.SendMessage(command.Text));
            }
            else if (command is CommandClient)
            {
                if (command.Type == CommandType.Create)
                    command.Connection.Async(() => command.Connection.SendCommand((Command)command.Data));
            }
            else if (command.Type == CommandType.Subscribe)
            {
                if (command.Connection == null)
                    throw new Exception("For subscribe command must be has connection");

                var dataObject = GetDataObject(command.Text);
                var notification = new Notification { CorrelationId = command.CorrelationId, DataObject = dataObject };
                command.Connection.Notifications.AddOrUpdate(command.CorrelationId, notification, (key, oldValue) => oldValue = notification);
                var resultData = GetData(dataObject);
                if (resultData != null)
                    command.Connection.Async(() =>
                    {
                        command.Connection.SendCommand(new Command
                            {
                                Type = CommandType.Create,
                                Text = command.Text,
                                CorrelationId = command.CorrelationId,
                                Data = resultData
                            });
                    });
            }
            else if (command.Type == CommandType.Unsubscribe)
            {
                if (command.Connection == null)
                    throw new Exception("For unsubscribe command must be has connection");
                Notification removeNotification;
                command.Connection.Notifications.TryRemove(command.CorrelationId, out removeNotification);
            }
            else
                switch (command.Type)
                {
                    case CommandType.Select:
                        command.SetResult(GetData(command.Text));
                        break;
                    case CommandType.Update:
                        UpdateData(command);
                        break;
                    default:
                        throw new Exception(String.Format("Unknown command '{0}'", command.Type));
                }
        }

        private void ProcessServiceQueue()
        {
            foreach (var command in serviceQueue.GetConsumingEnumerable(serviceTokenSource.Token))
            {
                if (command.CancellationToken.IsCancellationRequested)
                {
                    command.TaskCompletionSource.SetCanceled();
                    log.Trace(String.Format("Command cancel '{0}'", command.ToString()));
                }
                else
                    try
                    {
                        log.Trace(String.Format("Command start '{0}'", command.ToString()));
                        ProcessServiceQueue(command);
                        if (command.TaskCompletionSource != null && !command.TaskCompletionSource.Task.IsCompleted)
                            command.SetResult(null);
                        log.Trace(String.Format("Command stop '{0}'", command.ToString()));
                    }
                    catch (Exception ex)
                    {
                        if (command.TaskCompletionSource != null)
                            command.TaskCompletionSource.SetException(ex);
                        log.DebugException(String.Format("Command error '{0}': {1}", command.ToString(), ex.Message), ex);
                    }
                // TODO: ??? Подсчет статистики
                // Interlocked.Add(ref commandProcessed, 1);
            }
        }
    }
}