using System;
using System.Collections.Generic;
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
        string Connect(string userName, string password, string connectionId = null);

        [OperationContract]
        void Disconnect();

        [OperationContract]
        object Execute(Command command);

        // TODO: ???
        // [OperationContract(IsOneWay = true)]
        // void SendMessage(string message, string userName);
    }
}