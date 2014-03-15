using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Risk
{
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

        public void ReceiveMessage(string message)
        {
            MarshalInvoke(MethodInfo.GetCurrentMethod(), message);
        }

        public void ReceiveCommand(Command command)
        {
            MarshalInvoke(MethodInfo.GetCurrentMethod(), command);
        }
    }
}