﻿using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Risk
{
    /// <summary>
    /// WCF подключение к серверу
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, IncludeExceptionDetailInFaults = true)]
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ConnectionWCF : Connection
    {
        internal override void CheckConnection()
        {
            if (((ICommunicationObject)_callback).State != CommunicationState.Opened)
            {
                Disconnect();
            }
            base.CheckConnection();
        }

        public override ServerConnectionInfo Connect(string userName, string password, string connectionId)
        {
            OperationContext context = OperationContext.Current;
            MessageProperties prop = context.IncomingMessageProperties;
            RemoteEndpointMessageProperty endpoint = prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;

            _callback = context.GetCallbackChannel<IConnectionCallback>();

            ((ICommunicationObject)_callback).Faulted += (s, e) => Disconnect();
            ((ICommunicationObject)_callback).Closed += (s, e) => Disconnect();

            Address = endpoint.Address;
            Port = endpoint.Port;

            return base.Connect(userName, password, connectionId);
        }

        public override CommandResult Execute(Command command)
        {
            try
            {
                var result = base.Execute(command);

                // Check data type support WCF
                if (result != null && result.Data != null && !result.Data.GetType().IsValueType && !Command.KnownTypes().Contains(result.Data.GetType()))
                    throw new Exception(String.Format("WCF contract not supported result type '{0}'", result.Data.GetType().Name));

                return result;
            }
            catch (Exception ex)
            {
                throw new FaultException(ex.Message);
            }
        }
    }
}