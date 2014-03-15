using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class CommandData : Command
    {
        public CommandDataType Command { get; set; }
        public DataObject DataObject { get; set; }
        public string CorrelationId { get; set; }

        private CommandData()
        {
        }

        public static CommandData Create(Connection connection, CommandDataType command, string objectName, string correlationId = null)
        {
            if (String.IsNullOrWhiteSpace(objectName))
                throw new Exception("Data object name can not be null");

            DataObject _object;
            if (!Enum.TryParse<DataObject>(objectName, out _object))
                throw new Exception(String.Format("Unknown data object '{0}'", objectName));

            return new CommandData { Connection = connection, Command = command, DataObject = _object, CorrelationId = correlationId };
        }
    }

    public enum CommandDataType
    {
        Get,
        Subscribe,
        Unsubscribe
    }

    public enum DataObject
    {
        Clients,
        Portfolios,
        Connections
    }
}