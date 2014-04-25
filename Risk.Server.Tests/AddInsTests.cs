using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Risk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Risk.Tests
{
    [TestClass]
    public class AddInsTests
    {
        static AddInsTests()
        {
            Server.WithoutProcessQueue = true;
        }

        private class AddinTest : IAddIn
        {
            public static decimal Capital1 = 555;
            public static decimal Capital2 = 333;

            public string Name()
            {
                return "Test AddIn";
            }

            public string Version()
            {
                return "1.0";
            }

            public void Start(IServer server)
            {
                var portfolios = new Portfolio[] { new Portfolio { TradeCode = "1", Capital = Capital1 }, new Portfolio { TradeCode = "2", Capital = Capital2 } } ;
                server.Execute(Command.Insert("Portfolios", portfolios));
                Capital1 = 444;
                Capital2 = 222;
            }

            public void Stop()
            {
            }


            public void Execute(Command command)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void MainTest()
        {
            var typeName = typeof(AddinTest).FullName + ", " + typeof(AddinTest).Assembly.GetName();
            var server = new Server();
            server.Register(Server.Portfolios);
            server.AddIns.Register(typeName);
            server.Start();

            AddinTest.Capital1 = 444;
            AddinTest.Capital2 = 222;

            var result = ((IEnumerable<Portfolio>)server.Execute(Command.Select("Portfolios"))).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("1", result[0].TradeCode);
            Assert.AreEqual(555, result[0].Capital);
            Assert.AreEqual("2", result[1].TradeCode);
            Assert.AreEqual(333, result[1].Capital);

            server.AddIns.Stop(typeName);

            // Update AddIn version

            server.AddIns.Start(typeName);

            ((IEnumerable<Portfolio>)server.Execute(Command.Select("Portfolios"))).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("1", result[0].TradeCode);
            Assert.AreEqual(555, result[0].Capital);
            Assert.AreEqual("2", result[1].TradeCode);
            Assert.AreEqual(333, result[1].Capital);

            server.Stop();
        }
    }
}