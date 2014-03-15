using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [ServiceContract(CallbackContract = typeof(IConnectionCallback))]
    public interface IConnection
    {
        [OperationContract]
        string Connect(string userName, string password, string connectionId);

        [OperationContract]
        void Disconnect();

        [OperationContract]
        void Get(string name, string correlationId);

        [OperationContract]
        void SendMessage(string message);

        [OperationContract]
        void Subscribe(string name, string correlationId);

        [OperationContract]
        void Unsubscribe(string name, string correlationId);

        [OperationContract]
        IEnumerable<Client> GetClients();
    }
}