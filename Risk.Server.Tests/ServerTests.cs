using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Risk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Risk.Tests
{
    [TestClass()]
    public class ServerTests
    {
            [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
            public class ConnectionCallback : IConnectionCallback
            {            
                public void ReceiveMessage(string message)
                {
 	                throw new NotImplementedException();
                }

                public void ReceiveCommand(Command command)
                {
 	                throw new NotImplementedException();
                }
            }

        [TestMethod()]
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
        }
        [TestMethod()]
        public void CommandVersionTest()
        {
            var server = new Server();
            server.Start();
            var version = (new Command { Type = CommandType.Select, Text = "@@Version" }).Execute<string>();
            Assert.AreEqual(server.GetVersion(), version);
		}

        [TestMethod()]
        public void UpdateDataTradesTest()
        {
            var server = new Server();
            server.Start();

            var portfolios = new List<Portfolio>();
            portfolios.Add(new Portfolio
            {
                AccountId = 1,
                TradeCode = "MCR001",
                Currency = "RUR"
            });
            portfolios.Add(new Portfolio
            {
                AccountId = 2,
                TradeCode = "MCE002",
                Currency = "EUR"
            });
            var cmd = new Command { Type = CommandType.Update, Text = "Portfolios", Data = portfolios };
            cmd.Execute();

            var rates = new List<Rate>();
            rates.Add(new Rate 
            {
                CurrencyFrom = "EUR",
                CurrencyTo = "USD",
                Value = 1.39061M
            });
            rates.Add(new Rate
            {
                CurrencyFrom = "RUR",
                CurrencyTo = "USD",
                Value = 0.027397M
            });
            cmd = new Command { Type = CommandType.Update, Text = "Rates", Data = rates};
            cmd.Execute();

            var trades = new List<Trade>();
            trades.Add(new Trade 
            { 
                TradeCode = "MCR001",
                Price = 10,
                Quantity = 5,
                TradeNo = 1,
                OrderNo = 1,
                Sell = false,
                Value = 50,
                Seccode = "1"
            });
            trades.Add(new Trade
            {
                TradeCode = "MCR001",
                Price = 12,
                Quantity = 3,
                TradeNo = 2,
                OrderNo = 2,
                Sell = false,
                Value = 36,
                Seccode = "1"
            });
            cmd = new Command { Type = CommandType.Update, Text = "Trades", Data = trades };
            cmd.Execute();
            //Server.ExecuteCommand<IEnumerable<Trade>>(CommandType.Update, "Trades", trades);

            portfolios = server.Portfolios;

            server.Stop();
        }
    }
}
