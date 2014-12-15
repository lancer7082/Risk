using System.Collections.Generic;
using System.ServiceModel;
using Risk;

namespace Finam.NativeConnect
{
    /// <summary>
    /// Контракт подключения
    /// </summary>
    [ServiceContract(CallbackContract = typeof(IConnectionCallback))]
    public interface IConnection
    {
        [OperationContract]
        ServerConnectionInfo Connect(string userName, string password, string options = null);

        [OperationContract]
        void Disconnect();

        [OperationContract]
        CommandResult Execute(Command command);
    }
}