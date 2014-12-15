using System;
using Risk;

namespace Finam.NativeConnect
{
    /// <summary>
    /// Набор данных (аля TDataSet в Delphi)
    /// </summary>
    public class ExportDataSet : ExportRecordset, IExportDataSet, IDisposable
    {
        private bool _active;
        private DataUpdateEvent dataUpdateEvent;
        public string CorrelationId { get; set;}
        public string Text { get; set; }
        public string Filter { get; set; }

        public bool Active
        {
            get
            {
                return _active;
            }

            set
            {
                if (_active == value)
                    return;
                else if (_active)
                    Close();
                else
                    Open();
            }
        }

        public bool Notification { get; set; }

        public ExportDataSet(Connection connection, int instanceId)
            : base(connection, instanceId)
        {

        }

        public void Dispose()
        {
            if (Active)
                Active = false;
        }

        public void Open()
        {
            Connection.Trace("Start open DataSet '{0}'", Text);

            NewCorrelationId();  // For ignore old notification          

            Parameters["ObjectName"] = Text;
            Parameters["Filter"] = Filter;

            Connection.AddDataSet(this);

            try
            {
                var commandSelect = new Command { CommandText = Notification ? "Subscribe" : "Select", CorrelationId = CorrelationId, Parameters = Parameters };
                var commandResult = Connection.Execute(commandSelect);
                UpdateData(DataUpdateType.Create, commandResult.FieldsInfo, commandResult.Data);
                if (Active && dataUpdateEvent != null) // After restoring connection
                    dataUpdateEvent(InstanceId, (int)DataUpdateType.Create, null);
                _active = true;
            }
            catch (Exception ex)
            {
                if (Connection.State == ConnectionState.Active)
                    Connection.RemoveDataSet(this);
                throw ex;
            }
            finally
            {
                Connection.Trace("Stop open DataSet '{0}'", Text);
            }
        }

        public void Close()
        {
            Connection.Trace("Start close DataSet '{0}'", Text);
            _active = false;
            if (Connection.State == ConnectionState.Active)
                Connection.RemoveDataSet(this);
            Connection.Trace("Stop close DataSet '{0}'", Text);
        }

        private void NewCorrelationId()
        {
            CorrelationId = Guid.NewGuid().ToString();
        }

        public string GetText()
        {
            return Text;
        }

        public void SetText(string text)
        {
            this.Text = text;
        }

        public string GetFilter()
        {
            return Filter;
        }

        public void SetFilter(string filter)
        {
            filter = filter.Replace('\'', '"'); // TODO: !!! Change quoted symbol
            if (String.IsNullOrWhiteSpace(filter))
                filter = null;
            if ((Filter ?? "") != (filter ?? ""))
            {
                Filter = filter;
                if (Active)
                    Open();
            }
        }

        public bool GetNotifications()
        {
            return Notification;
        }

        public void SetNotifications(bool notification)
        {
            if (Active && notification)
                throw new Exception("Can not change notification on active command");
            this.Notification = notification;
        }

        private object updateSync = new object();

        public void ReceiveCommand(Command command)
        {
            lock (updateSync)
            {
                var dataUpdateType = (DataUpdateType)Enum.Parse(typeof(DataUpdateType), command.CommandText, true);
                var updatedIndexes = UpdateData(dataUpdateType, command.FieldsInfo, command.Data);
                if (Active && dataUpdateEvent != null)
                    dataUpdateEvent(InstanceId, (int)dataUpdateType, updatedIndexes);
            }
        }

        public void OnDataUpdate(DataUpdateEvent dataUpdateEvent)
        {
            this.dataUpdateEvent += dataUpdateEvent;
        }
    }

    public enum DataUpdateType
    {
        Create = 0,
        Insert = 1,
        Update = 2,
        Delete = 3
    }
}