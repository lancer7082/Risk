using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Risk.Tests
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                // Тестирование подключения к транзаку

                var serverUri = new Uri("net.tcp://localhost:2123/Risk");

                // Start server
                var server = new Server();
                server.ServiceHost = new ServiceHost(typeof(ConnectionWCF));
                var binding = new NetTcpBinding(SecurityMode.None);
                ServiceDescription serviceDesciption = server.ServiceHost.Description;
                if (serviceDesciption.Endpoints.Count == 0)
                    server.ServiceHost.AddServiceEndpoint(typeof(IConnection), binding, serverUri.ToString());
                server.ProcessCount = 1;

                server.Register(Server.Clients);
                server.Register(Server.Portfolios);
                server.Register(Server.ExchangeRates);
                server.Register(Server.Trades);
                server.Register(Server.Positions);
                server.Register(Server.PositionsInstruments);
                server.Register(Server.Alerts);
                //server.Register(Server.Orders);
                server.Register(Server.PortfolioRules);
                //server.Register(Server.Instruments);
                //server.Register(Server.InstrumentGroups);

                server.AddIns.Register("Risk.Transaq.TransaqAddIn, Risk.Transaq", 0);

                server.Start();
                //server.AddIns.Start("Risk.Transaq.TransaqAddIn, Risk.Transaq");

                /*
                Server.Portfolios.Add(
                //server.Execute(Command.Insert("Portfolios", 
                    new Portfolio 
                    {
                        TradeCode = "MCE1026"
                    });
                */

                Console.WriteLine("To stop, press any key . . .");
                Console.ReadKey();

                server.Stop();
                server = null;

                Console.WriteLine("To exit, press any key . . .");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("To exit, press any key . . .");
                Console.ReadKey();
            }
        }
    }
}
