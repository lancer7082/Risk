using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    // TODO: ??? DataContractResolver http://stackoverflow.com/questions/11292357/wcf-datacontractresolver
    [ServiceKnownType(typeof(Type))]
    [ServiceKnownType(typeof(Client))]
    [ServiceKnownType(typeof(Portfolio))]
    [ServiceKnownType(typeof(ConnectionInfo))]
    public interface IConnectionCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveMessage(string message);
        
        [OperationContract(IsOneWay = true)]
        void Notify(string action, string correlationId, string type, IEnumerable<object> items);
    }
}