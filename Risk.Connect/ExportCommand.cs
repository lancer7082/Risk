using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class ExportCommand : IExportCommand
    {
        private int _instanceId;
        private Connection _connection;
        private Command _command;
        private ExportParameters _parameters;

        public ExportData Data { get; private set; }

        public IExportData GetData()
        {
            return Data;
        }

        public ExportCommand(Connection connection, Command command, int instanceId)
        {
            _connection = connection;
            _command = command;
            _parameters = new ExportParameters(command.Parameters);
            _instanceId = instanceId;
            Data = new ExportData(connection);
        }

        public void Execute()
        {
            _connection.Execute(_command);
            Data.UpdateData(_command.Data);
        }

        public string GetCommandText()
        {
            return _command.CommandText;
        }

        public void SetCommandText(string commandText)
        {
            _command.CommandText = commandText;
        }

        public IExportParameters GetParameters()
        {
            return _parameters;
        }

    }
}
