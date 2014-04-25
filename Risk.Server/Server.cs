using System;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;

namespace Risk
{
    /// <summary>
    /// Сервер
    /// </summary>
    public class Server : ServerBase
    {
        private byte firmId;

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
        /// Настройки
        /// </summary>
        public static RiskSettings Settings;

        // Timers TODO: !!! Перенести на Jobs
        private Timer timerClients;
        private Timer timerPortfolios;
        private Timer timerMoneyInOutDay;
        private Timer timerRates;
        private Timer timerInstruments;
        private Timer timerCheckTransaqPrices;

        public override void Configure()
        {
            base.Configure();

            // Configure DataBase
            var css = ConfigurationManager.ConnectionStrings["Risk"];
            if (css == null)
                throw new Exception("Not found connection string 'Risk' in config");
            var firmIdConfig = ConfigurationManager.AppSettings["FirmId"];
            if (firmIdConfig == null)
                throw new Exception("Not found AppSettings 'FirmId' in config");
            firmId = byte.Parse(firmIdConfig);
            DataBase = new DataBase(css.ConnectionString, firmId);
            DataBase.CommandTimeout = 60 * 5;

            // Configure Settings
            Settings = DataBase.ReadSettings() ?? new RiskSettings();

            Register(new RiskSettingsView());

            // Register tables
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

            // Получение данных по инструментам
            timerInstruments = new Timer(c =>
            {
                log.Trace("Start job 'Update Instruments from SQL' : {0}", css.ConnectionString);
                try
                {
                    lock (DataBase)
                    {
                        new CommandMerge
                        {
                            Object = Instruments,
                            Data = DataBase.GetInstruments(ServerTime),
                            Fields = "Decimals,Bpcost,Lotsize",
                        }.Execute();
                    }
                    log.Info("Success job 'Update Instruments from SQL'");
                }
                catch (Exception ex)
                {
                    log.ErrorException(ex.Message, ex);
                }
            }, null, 0, 300 * 1000);

            // Получение курсов
            // TODO: !!! Jobs.Add("Update ExchangeRates from SQL", 0, 120 * 1000, x =>
            timerRates = new Timer(c =>
            {
                log.Trace("Start job 'Update ExchangeRates from SQL' : {0}", css.ConnectionString);
                try
                {
                    lock (DataBase)
                    {
                    }
                    log.Info("Success job 'Update ExchangeRates from SQL'");
                }
                catch (Exception ex)
                {
                    log.ErrorException(ex.Message, ex);
                }
            }, null, 0, 120 * 1000);

            // TODO: !!! Jobs.Add("Update Portfolios from SQL", 0, 300 * 1000, x =>
            timerPortfolios = new Timer(c =>
            {
                log.Trace("Start job 'Update Portfolios from SQL' : {0}", css.ConnectionString);
                try
                {
                    lock (DataBase)
                    {
                        new CommandMerge
                        {
                            Object = Portfolios,
                            Data = DataBase.GetPortfolios(),
                            Fields = "AccountId,Client,CodeWord,Currency,MoneyInInit,MoneyOutInit,Active,Contragent,FinRes",
                        }.Execute();
                    }
                    log.Info("Success job 'Update Portfolios from SQL'");
                }
                catch (Exception ex)
                {
                    log.ErrorException(ex.Message, ex);
                }
            }, null, 0, 180 * 1000);

            // TODO: !!! Jobs.Add("Update Portfolios MoneyInOutDay from SQL", 0, 300 * 1000, x =>
            timerMoneyInOutDay = new Timer(c =>
            {
                log.Trace("Start job 'Update Portfolios MoneyInOutDay from SQL' : {0}", css.ConnectionString);
                try
                {
                    lock (DataBase)
                    {
                        new CommandUpdate
                        {
                            Object = Portfolios,
                            Data = DataBase.GetMoneyInOutDay(),
                            Fields = "MoneyInDay,MoneyOutDay",
                        }.Execute();
                    }
                    log.Info("Success job 'Update Portfolios MoneyInOutDay from SQL'");
                }
                catch (Exception ex)
                {
                    log.ErrorException(ex.Message, ex);
                }
            }, null, 0, 120 * 1000);

            // Проверка получения рыночных данных из Transaq
            // по выбранным инструментам
            timerCheckTransaqPrices = new Timer(c =>
            {
                log.Trace("Start job 'Check Transaq prices' : {0}", css.ConnectionString);
                try
                {
                    lock (DataBase)
                    {
                        string instruments = "";
                        byte status = 0;
                        if (DataBase.CheckTransaqPrices(ref instruments, ref status) == 0)
                        {
                            var alerts = new Alert[] {
                                new Alert 
                                { 
                                    DateTime = Server.Current.ServerTime,                                
                                    Text = String.Format("Рыночные данные по инструментам {0} не получены", instruments),                                 
                                }
                            };

                            new CommandInsert
                            {
                                Object = Server.Alerts,
                                Data = alerts,
                            }.ExecuteAsync();
                        }
                    }
                    log.Info("Success job 'Check Transaq prices'");
                }
                catch (Exception ex)
                {
                    log.ErrorException(ex.Message, ex);
                }
            }, null, 0, 180 * 1000);

            // TODO: !!! Jobs.Add("Update Clients from SQL", 0, 300 * 1000, x =>
            timerClients = new Timer(c =>
            {
                log.Trace("Start job 'Update Clients from SQL' : {0}", css.ConnectionString);
                try
                {
                    lock (DataBase)
                    {
                        new CommandMerge
                        {
                            Object = Clients,
                            Data = DataBase.GetClients()
                        }.Execute();
                    }
                    log.Info("Success job 'Update Clients from SQL'");
                }
                catch (Exception ex)
                {
                    log.ErrorException(ex.Message, ex);
                }
            }, null, 0, 120 * 1000);

            /*
            // Тестирование оповещений
            timerRates = new Timer(c =>
            {
                new CommandMessage { MessageType = MessageType.Info, Message = "Test" }.ExecuteAsync();

            }, null, 0, 10 * 1000);
            */

            /*
            // Тестовые позиции
            var positions = new System.Collections.Generic.List<Position>();
            positions.Add(new Position
            { 
                AccountId = 1, TradeCode = "1", Balance = 100, Bought = 0, Sold = 100, SecCode = "A", 
            });
            (new CommandServer
            {
                DataObject = Positions,
                //Text = "Positions",
                Data = positions,
                Fields = new string[] { "AccountId", "TradeCode", "SecCode", "Bought", "Sold", "Balance", "PL", "SecurityCurrency" },
                MergeKeyFields = new string[] { "TradeCode" },
            }).ExecuteAsync();
            //*/

            /*
            // Тестовые поручения
            //var orders = new System.Collections.Generic.List<Order>();
            var order = new Order()
            {
                Date = ServerTime,
                OrderId = 1,
                OrderNo = 1,
                OrderType = OrderType.Buy,
                Price = 0,
                Quantity = 1,
                SecСode = "A",
                TradeCode = "B",
            };
            new CommandInsert
            {
                Object = Orders,
                Data = order,
            }.ExecuteAsync();
            //*/

            // Configure AddIns
            AddIns.Register("Risk.Transaq.TransaqAddIn, Risk.Transaq", 0);

            // Configure WCF
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
                ServiceHost.AddServiceEndpoint(typeof(IConnection), binding, NetTCPUri.ToString());
            };

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
    }
}