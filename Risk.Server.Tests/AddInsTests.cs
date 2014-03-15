using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Risk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Risk.Tests
{
    [TestClass()]
    public class AddInsTests
    {
        private class AddinTest : IAddIn
        {
            public static double Capital1 = 555;
            public static double Capital2 = 333;

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
                var portfolios = new Portfolio[] { new Portfolio { AccountId = 1, Capital = Capital1 }, new Portfolio { AccountId = 2, Capital = Capital2 } } ;
                server.Execute(new Command { Type = CommandType.Update, Text = "Portfolios", Data = portfolios });
                Capital1 = 444;
                Capital2 = 222;
            }

            public void Stop()
            {
            }
        }

        [TestMethod()]
        public void MainTest()
        {
            var typeName = typeof(AddinTest).FullName + ", " + typeof(AddinTest).Assembly.GetName();
            var server = new Server();
            server.AddIns.Register(typeName);
            server.ProcessCount = 1;
            server.Start();

            AddinTest.Capital1 = 444;
            AddinTest.Capital2 = 222;

            var result = (new Command { Type = CommandType.Select, Text = "Portfolios" }).Execute <IEnumerable <Portfolio>>().ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].AccountId);
            Assert.AreEqual(555, result[0].Capital);
            Assert.AreEqual(2, result[1].AccountId);
            Assert.AreEqual(333, result[1].Capital);

            server.AddIns.Stop(typeName);

            // Update AddIn version

            server.AddIns.Start(typeName);

            result = (new Command { Type = CommandType.Select, Text = "Portfolios" }).Execute<IEnumerable<Portfolio>>().ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].AccountId);
            Assert.AreEqual(555, result[0].Capital);
            Assert.AreEqual(2, result[1].AccountId);
            Assert.AreEqual(333, result[1].Capital);

            server.Stop();
        }
    }
}