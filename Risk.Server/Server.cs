using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using Risk.Commands;
using Risk.Configuration;

namespace Risk
{
    /// <summary>
    /// Сервер
    /// </summary>
    public class Server : ServerBase
    {
        public const string ETNAAddInName = "Finam.AddIns.ETNA.ETNAAddIn, Finam.AddIns.ETNA";

        public string ConnectionString { get; set; }

        public byte? FirmId { get; set; }

        public string TransaqUsaHostName { get; set; }

        public int DatabaseCommandTimeout { get; set; }

        public int AutoMarginCallInterval { get; set; }

        /// <summary>
        /// Клиенты (только для теста)
        /// </summary>
        public static readonly Clients Clients = new Clients();

        /// <summary>
        /// Портфели клиентов
        /// </summary>
        public static readonly Portfolios Portfolios = new Portfolios();

        /// <summary>
        /// Курсы валют
        /// </summary>
        public static readonly ExchangeRates ExchangeRates = new ExchangeRates();

        /// <summary>
        /// Сделки
        /// </summary>
        public static readonly Trades Trades = new Trades();

        /// <summary>
        /// Позиции
        /// </summary>
        public static readonly Positions Positions = new Positions();

        /// <summary>
        /// Позиции в разрезе инструментов
        /// </summary>
        public static readonly PositionsInstruments PositionsInstruments = new PositionsInstruments();

        /// <summary>
        /// Оповещения
        /// </summary>
        public static readonly Alerts Alerts = new Alerts();

        /// <summary>
        /// Поручения
        /// </summary>
        public static readonly Orders Orders = new Orders();

        /// <summary>
        /// Таблица портфелей, по которым сработали правила
        /// </summary>
        public static readonly PortfolioRules PortfolioRules = new PortfolioRules();

        /// <summary>
        /// Инструменты
        /// </summary>
        public static readonly Instruments Instruments = new Instruments();

        /// <summary>
        /// Группы инструментов
        /// </summary>
        public static readonly InstrumentGroups InstrumentGroups = new InstrumentGroups();

        /// <summary>
        /// Информация о ставках ГО инструментов
        /// </summary>
        public static readonly InstrumentsGOInfo InstrumentsGOInfo = new InstrumentsGOInfo();

        /// <summary>
        /// Финансовый результат
        /// </summary>
        public static readonly FinancialResults FinancialResults = new FinancialResults();

        /// <summary>
        /// AutoMarginCallInfos
        /// </summary>
        public static readonly AutoMarginCallInfos AutoMarginCallInfos = new AutoMarginCallInfos();

        /// <summary>
        /// AccountsLimits
        /// </summary>
        public static readonly AccountsLimits AccountsLimits = new AccountsLimits();

        /// <summary>
        /// Настройки
        /// </summary>
        public static RiskSettings Settings;

        private readonly AutoMarginCall _autoMarginCall = new AutoMarginCall();

        private readonly CheckInstrumentsQuotes _checkInstrumentsQuotes = new CheckInstrumentsQuotes();

        private readonly IODailyMonitoring _IODailyMonitoring = new IODailyMonitoring();

        private readonly CheckScalperTrades _checkScalperTrades = new CheckScalperTrades();

        /// <summary>
        /// Загрузка конфигурации
        /// </summary>
        public void LoadConfig()
        {
            ServerConfigurationSection = RiskServerSection.GetSection<RiskServerSection>("riskServer", this);
            ServerConfigurationSection.ApplyConfigToObject();

            if (!FirmId.HasValue)
                throw new Exception("Not found 'firmId' in config");
            if (string.IsNullOrEmpty(ConnectionString))
                throw new Exception("Not found connection string in config");
        }

        public override void Configure()
        {
            LoadConfig();

            base.Configure();

            // Configure DataBase
            DataBase = new DataBase(ConnectionString, FirmId.Value)
            {
                CommandTimeout = DatabaseCommandTimeout
            };

            // Configure Settings
            Settings = DataBase.ReadSettings() ?? new RiskSettings();

            Register(new RiskSettingsView());

            // Register tables
            Register(new TestTable());
            Register(new TestTableResultInfo());
            Register(Clients);
            Register(Portfolios);
            Register(ExchangeRates);
            Register(Trades);
            Register(Positions);
            Register(PositionsInstruments);
            Register(Alerts);
            Register(Orders);
            Register(PortfolioRules);
            Register(Instruments);
            Register(InstrumentGroups);
            Register(InstrumentsGOInfo);
            Register(FinancialResults);
            Register(AutoMarginCallInfos);
            Register(AccountsLimits);

            LoadOrders();

            JobManager.StartAllJobs();

            // Configure WCF
            InitWCFConnection();

            _checkInstrumentsQuotes.Start();
            _IODailyMonitoring.Start();
            _autoMarginCall.Start(AutoMarginCallInterval);
            _checkScalperTrades.Start();

            ThreadPool.QueueUserWorkItem(state =>
            {
                while (true)
                {
                    new CommandGetAccountsLimits().Execute();
                    Thread.Sleep(15 * 60 * 1000);
                }
            });

            ServerBase.Current.JobManager.RestartJob("LoadInstrumentsGOInfo");

            //ThreadPool.QueueUserWorkItem(state =>
            //{
            //    Thread.Sleep(10 * 1000);
            //    while (true)
            //    {
            //        var alert = new Alert
            //        {
            //            DateTime = DateTime.Now,
            //            Text = "Test",
            //            AlertType = AlertType.NewPositionInMarginCall
            //        };

            //        new CommandInsert
            //        {
            //            Object = Server.Alerts,
            //            Data = alert,
            //        }.Execute();

            //        Thread.Sleep(10 * 1000);
            //    }
            //});


            //ThreadPool.QueueUserWorkItem(state =>
            //{
            //    Thread.Sleep(2 * 60 * 1000);
            //    while (true)
            //    {
            //        var cmd = new CommandRestartJob();
            //        cmd.Parameters = new ParameterCollection
            //        {
            //            new Parameter
            //            {
            //                Name = "JobName",
            //                Value = "LoadInstrumentsGOInfo"
            //            }
            //        };
            //        cmd.Execute();
            //        Thread.Sleep(60 * 1000);
            //    }
            //});

            // TODO: ???
            //foreach (ServiceEndpoint endpoint in serviceDesciption.Endpoints)
            //{
            //    endpoint.Behaviors.Add(new ConnectionEndpointBehaviour(this));
            //}

            // TODO: ???
            // Service description
            //ServiceMetadataBehavior smb = serviceHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
            //if (smb == null)
            //{
            //    smb = new ServiceMetadataBehavior();
            //    serviceHost.Description.Behaviors.Add(smb);
            //}
            //smb.HttpGetEnabled = true;
            //serviceHost.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
        }

        /// <summary>
        /// Инициализация WCFConnection
        /// </summary>
        protected override void InitWCFConnection()
        {
            ServiceHost = new ServiceHost(typeof(ConnectionWCF) /*, new Uri("http://localhost:8001/Risk") */);
            ServiceDescription serviceDesciption = ServiceHost.Description;
            if (serviceDesciption.Endpoints.Count == 0)
            {
#if DEBUG
                var NetTCPUri = new Uri("net.tcp://localhost:26455/Risk");
#else
                var NetTCPUri = new Uri("net.tcp://bofm.finam.ru:26455/Risk");
#endif
                var binding = new NetTcpBinding(SecurityMode.None);
                binding.ReceiveTimeout = new TimeSpan(0, 5, 0);
                binding.SendTimeout = new TimeSpan(0, 5, 0);
                ServiceHost.AddServiceEndpoint(typeof(IConnection), binding, NetTCPUri.ToString());
            }
        }

        /// <summary>
        /// Загрузка поручений
        /// </summary>
        private void LoadOrders()
        {
            try
            {
                var orders = DataBase.LoadOrders();

                ProcessCommand(new CommandInsert
                {
                    Object = Orders,
                    Data = orders
                });
            }
            catch (Exception e)
            {
                Log.ErrorException(String.Format("Can't load orders: {0}", e.Message), e);
            }
        }
    }
}