using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ConnectionCallback : IConnectionCallback
    {
        private Connection connection;

        public ConnectionCallback(Connection connection)
        {
            this.connection = connection;
        }

        public void ReceiveMessage(string message)
        {
            connection.ReceiveMessage(message);
        }

        private static IEnumerable CastIterator<TResult>(IEnumerable source)
        {
            IEnumerator enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                yield return (TResult)current;
            }
        }

        public void Notify(string action, string correlationId, string type, IEnumerable<object> items)
        {
            var elementType = Type.GetType(type + ", Risk.Common", false);

            if (elementType == null)
                connection.ReceiveMessage(String.Format("ERROR: Unknown type {0}", type));

            connection.ReceiveCommand(new Command(connection, action, correlationId, items, elementType));
        }
    }
}