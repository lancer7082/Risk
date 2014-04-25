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
    }
}
