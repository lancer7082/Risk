using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Набор данных (аля TDataSet в Delphi)
    /// </summary>
    public class ExportDataSet : IExportDataSet, IDisposable
    {
        private ExportParameters _exportParameters;
        private int _instanceId;
        private bool _active;
        private DataUpdateEvent dataUpdateEvent;

        public Connection Connection { get; private set; }       
        public string CorrelationId { get; set;}
        public string ObjectName { get; set; }
        public string Filter { get; set; }
        public ParameterCollection Parameters { get; private set; }

        public IExportParameters GetParameters()
        {
            return _exportParameters;
        }

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

        public ExportData Data { get; private set; }

        public IExportData GetData()
        {
            return Data;
        }

        public ExportDataSet(Connection connection, int instanceId)
        {
            this.Parameters = new ParameterCollection();
            this._exportParameters = new ExportParameters(Parameters);
            this.Connection = connection;
            this._instanceId = instanceId;
            this.Data = new ExportData(Connection);
        }

        public void Dispose()
        {
            if (Active)
                Active = false;
        }

        public void Open()
        {
            Connection.Trace("Start open DataSet '{0}'", ObjectName);

            NewCorrelationId();  // For ignore old notification          

            Parameters["ObjectName"] = ObjectName;
            Parameters["Filter"] = Filter;

            if (!Connection.ActiveDataSets.Contains(this))
                Connection.ActiveDataSets.Add(this);

            try
            {
                var commandSelect = new Command { CommandText = Notification ? "Subscribe" : "Select", CorrelationId = CorrelationId, Parameters = Parameters };                
                Connection.Execute(commandSelect);
                Data.UpdateData(commandSelect.Data);
                _active = true;
            }
            catch (Exception ex)
            {
                Connection.ActiveDataSets.Remove(this);
                throw ex;
            }
            finally
            {
                Connection.Trace("Stop open DataSet '{0}'", ObjectName);
            }
        }

        public void Close()
        {
            Connection.Trace("Start close DataSet '{0}'", ObjectName);
            _active = false;
            if (Connection.State == ConnectionState.Active && Connection.ActiveDataSets.Contains(this))
            {
                Connection.ActiveDataSets.Remove(this);
                if (Notification)
                {
                    Connection.Execute(new Command
                    {
                        CommandText = "Unsubscribe",
                        CorrelationId = CorrelationId,
                        Parameters = { new Parameter("ObjectName", ObjectName) }
                    });
                }
            }
            Connection.Trace("Stop close DataSet '{0}'", ObjectName);
        }

        private void NewCorrelationId()
        {
            CorrelationId = Guid.NewGuid().ToString();
        }

        public string GetObjectName()
        {
            return ObjectName;
        }

        public void SetObjectName(string objectName)
        {
            this.ObjectName = objectName;
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

        public void ReceiveCommand(Command command)
        {
            Data.UpdateData(command.Data);

            // TODO: ??? UpdateData(command.Data);

            int dataUpdateType;
            switch (command.CommandText.ToUpper())
            {
                case "CREATE": dataUpdateType = 0; break;
                case "INSERT": dataUpdateType = 1; break;
                case "UPDATE": dataUpdateType = 2; break;
                case "DELETE": dataUpdateType = 3; break;
                default: return;
            }

            if (Active && dataUpdateEvent != null)
                dataUpdateEvent(_instanceId, dataUpdateType);
        }

        public void OnDataUpdate(DataUpdateEvent dataUpdateEvent)
        {
            this.dataUpdateEvent += dataUpdateEvent;
        }
    }
}