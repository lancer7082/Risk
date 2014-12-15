using System;
using System.Linq;
using System.Reflection;

namespace Risk
{
    /// <summary>
    /// Маршаллинг контракта на произвольный класс
    /// </summary>
    [Serializable]
    public class MarshalConnectionCallback : IConnectionCallback
    {
        private Type instanceType;
        private object instance;

        public MarshalConnectionCallback(object instance)
        {
            if (instance == null)
                throw new Exception("Instance can not be null");
            this.instance = instance;
            instanceType = instance.GetType();
        }

        private void MarshalInvoke(MethodBase method, params object[] parameters)
        {
            try
            {
                var pars = from p in method.GetParameters()
                           select p.ParameterType;
                MethodInfo marshalMethod = instanceType.GetMethod(method.Name, pars.ToArray());
                if (marshalMethod != null)
                    marshalMethod.Invoke(instance, parameters);
            }
            catch
            {
                // Ignore all errors
            }
        }

        public void ReceiveMessage(string message, MessageType messageType)
        {
            MarshalInvoke(MethodInfo.GetCurrentMethod(), message, messageType);
        }

        public void ReceiveCommand(Command command)
        {
            MarshalInvoke(MethodInfo.GetCurrentMethod(), command);
        }
    }
}