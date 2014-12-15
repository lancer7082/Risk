using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Risk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.IO;
using System.Configuration;

namespace Risk.Tests
{
    [TestClass]
    public class ServerTests
    {
        static ServerTests()
        {
            Server.WithoutProcessQueue = true;
        }

        [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
        public class ConnectionCallback : IConnectionCallback
        {
            public void ReceiveMessage(string message, MessageType messageType)
            {
                throw new NotImplementedException();
            }

            public void ReceiveCommand(Command command)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void ConnectTest()
        {
            var serverUri = new Uri("net.tcp://localhost:2123/Risk");

            // Start server
            var server = new Server();
            server.ServiceHost = new ServiceHost(typeof(ConnectionWCF));
            var binding = new NetTcpBinding(SecurityMode.None);
            ServiceDescription serviceDesciption = server.ServiceHost.Description;
            if (serviceDesciption.Endpoints.Count == 0)
                server.ServiceHost.AddServiceEndpoint(typeof(IConnection), binding, serverUri.ToString());
            server.ProcessCount = 1;
            server.Start();

            // Start client
            var context = new InstanceContext(new ConnectionCallback());
            binding.MaxReceivedMessageSize = int.MaxValue;
            var factory = new DuplexChannelFactory<IConnection>(context, binding);
            EndpointAddress endpoint = new EndpointAddress(serverUri);
            var connection = factory.CreateChannel(endpoint);
            connection.Connect("test", "Test");
            connection.Disconnect();
        }

        [TestMethod]
        public void PositionsInstrumentsTest()
        {
            const string testSecCode = "TestSecCode";
            const string testTradeCode = "TestTradeCode";

            var testBalance = 100;
            var testPL = 1000;

            var server = new Server();
            server.Register(Server.Positions);
            server.Register(Server.PositionsInstruments);
            server.ProcessCount = 1;
            server.Start();


            TestPositionsInstruments(testBalance, testPL, testTradeCode, testSecCode, server);
            TestPositionsInstruments(++testBalance, ++testPL, testTradeCode, testSecCode, server);
        }

        [TestMethod]
        public void PortfoliosSubscribeWithLikeTest()
        {
            var server = new Server();
            server.Register(Server.Portfolios);
            server.ProcessCount = 1;
            server.Start();

            var data = new CommandSubscribe()
            {
                Object = Server.Portfolios,
                Parameters = new ParameterCollection()
                {
                    new Parameter("ObjectName", "Portfolios"),
                    new Parameter("Filter", "(TradeCode LIKE \"ntr\")")
                }
            }.Execute();
        }

        [TestMethod]
        public void AlertsSelectWithFilterTest()
        {
            var server = new Server();
            server.Register(Server.Alerts);
            server.ProcessCount = 1;
            server.Start();

            var data = new CommandSubscribe()
            {
                Object = Server.Alerts,
                Parameters = new ParameterCollection
                {
                    new Parameter("ObjectName", "Alerts"),
                    new Parameter("Filter", "(TradeCode = \"MCR1056\")")
                }
            }.Execute();
        }


        [TestMethod]
        public void AutoMarginCallTest()
        {
            var autoMarginCallInfo = new AutoMarginCallInfo();
            var accountId = 100;
            var currentCapital = 15000;
            Server.Settings = new RiskSettings
            {
                PlannedCapitalUtilization = 190
            };

            var capitalUsagesEthalon = new[] { 328, 208, 190 };
            var quantitiesForCloseEtalon = new[] { 300, 170, 0 };
            var GOPositionsEtalon = new[] { 0, (decimal)13487.5, 0 };

            var positions = new List<Position>
            {
                new Position
                {
                    AccountId = accountId,
                    Balance = 300,
                    SecCode = "I1",
                    GOPos = 18000,
                    GORateLong = (decimal) (0.5),
                    Quote = 120
                },
                new Position
                {
                    AccountId = accountId,
                    Balance = 1000,
                    SecCode = "I2",
                    GOPos = 16250,
                    GORateLong = (decimal) (0.125),
                    Quote = 130
                },
                new Position
                {
                    AccountId = accountId,
                    Balance = 10000,
                    SecCode = "I3",
                    GOPos = 15000,
                    GORateLong = (decimal) (0.1),
                    Quote = 15
                }
            };

            for (var i = 0; i < positions.Count; i++)
            {

                var currentCapitalUsage = (int)((positions.Sum(p => p.GOPos) / currentCapital) * 100);

                if (i == 2)
                {
                    Assert.IsTrue(currentCapitalUsage <= Server.Settings.PlannedCapitalUtilization);
                    return;
                }
                var positionPrice = positions[i].Quote * (decimal)Math.Pow(10, 5) * 1 / 100000;
                var quantityForClose = AutoMarginCall.CalculateInstrumentQuantity(currentCapital, positions[i], positionPrice, positions, autoMarginCallInfo);
                Assert.IsTrue(capitalUsagesEthalon[i] == currentCapitalUsage);
                Assert.IsTrue(quantitiesForCloseEtalon[i] == quantityForClose.Value);
                positions[i].Balance -= quantityForClose.Value;
                positions[i].GOPos = positions[i].Balance * positions[i].Quote * positions[i].GORate;
                Assert.IsTrue(GOPositionsEtalon[i] == positions[i].GOPos);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="testBalance"></param>
        /// <param name="testPL"></param>
        /// <param name="testTradeCode"></param>
        /// <param name="testSecCode"></param>
        /// <param name="server"></param>
        private static void TestPositionsInstruments(int testBalance, int testPL, string testTradeCode, string testSecCode, Server server)
        {
            var positions = new List<Position>
            {
                new Position()
                {
                    AccountId = 1,
                    Balance = testBalance,
                    PLCurrencyCalc = testPL,
                    TradeCode = testTradeCode,
                    SecCode = testSecCode,
                    
                },
                new Position()
                {
                    AccountId = 1,
                    Balance = testBalance,
                    PLCurrencyCalc = testPL,
                    TradeCode = testTradeCode+testTradeCode,
                    SecCode = testSecCode
                }
            };

            server.Execute(Command.Merge("Positions", positions, "AccountId,OpenBalance,Bought,Sold,Balance,PL,GOPos,SecurityCurrency", "TradeCode"));

            var position = Server.Positions.Where(s => s.SecCode == testSecCode).ToList();
            var positionInstrument = Server.PositionsInstruments.FirstOrDefault(s => s.SecCode == testSecCode);

            Assert.IsNotNull(position);
            Assert.IsNotNull(positionInstrument);

            Assert.AreEqual(position.Sum(s => s.Balance), testBalance * 2);
            Assert.AreEqual(positionInstrument.Balance, testBalance * 2);

            //Assert.AreEqual(position.Sum(s => s.PLCurrencyCalc), testPL * 2);
            //Assert.AreEqual(positionInstrument.PLCurrencyCalc, testPL * 2);
        }
    }
}
