using System;
using Risk;

namespace Finam.NativeConnect
{
    public class ExportCommand : ExportRecordset, IExportCommand
    {
        private Command _command;

        public ExportCommand(Connection connection, Command command, int instanceId)
            : base(connection, instanceId)
        {
            _command = command;
        }

        public void Execute()
        {
            if (InstanceId == -1) // From server
            {
                UpdateData(DataUpdateType.Create, _command.FieldsInfo, _command.Data);
            }
            else
            {
                _command.Parameters = Parameters;
                var commandResult = Connection.Execute(_command);
                UpdateData(DataUpdateType.Create, commandResult.FieldsInfo, commandResult.Data);
            }
        }

        public string GetText()
        {
            return _command.CommandText;
        }

        public void SetText(string commandText)
        {
            _command.CommandText = commandText;
        }
    }
}
