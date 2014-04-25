using System.ServiceModel;

namespace Risk
{
    /// <summary>
    /// Обратный контракт при дуплексном общении
    /// </summary>
    public interface IConnectionCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveMessage(string message, MessageType messageType);

        [OperationContract(IsOneWay = true)]
        void ReceiveCommand(Command command);
    }
}