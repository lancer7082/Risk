using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public interface IConnectionCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveMessage(string message);

        [OperationContract(IsOneWay = true)]
        void ReceiveCommand(Command command);
    }
}