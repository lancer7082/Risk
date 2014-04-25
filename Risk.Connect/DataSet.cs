using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Risk
{
    /// <summary>
    /// Набор данных (аля TDataSet в Delphi)
    /// </summary>
    public class DataSet : IExportDataSet, IDisposable
    {
        private Dictionary<string, PropertyInfo> fields = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
        private IEnumerator enumerator;
        private bool _eof;
        private ReceiveCommand receiveEvent;
        private int instanceId;

        public Connection Connection { get; private set; }
        public Command Command { get; private set; }
        
        public string CorrelationId
        {
            get { return Command.CorrelationId; }
        }

        public string Text
        {
            get { return Command.Text; }
        }

        public bool Active { get; internal set; }
        public bool Notification { get; set; }

        public DataSet(Connection connection, int instanceId)
        {
            this.Connection = connection;
            this.instanceId = instanceId;
            Command = new Command();
        }

        public DataSet(Connection connection, int instanceId, Command command)
        {
            this.Connection = connection;
            this.instanceId = instanceId;
            Command = command;
            UpdateData(Command.Data);
        }

        public void Dispose()
        {
            if (Active)
                Connection.CloseCommand(this);
            // TODO: ??? Thread.MemoryBarrier();
        }

        private object _data;

        private void UpdateData(object data)
        {
            this._data = data;

            // TODO: ??? Кешировать разбор полей для типа
            fields.Clear();

            First();

            if (data == null || data.GetType().IsValueType || data is string)
                return;

            Type dataType = null; // TODO: !!! Get from command

            if (dataType == null)
            {
                dataType = data.GetType();
                if (data is IEnumerable)
                {
                    var ienum = dataType.GetInterface(typeof(IEnumerable<>).Name);
                    if (ienum != null)
                        dataType = ienum.GetGenericArguments()[0];
                }
            }

            foreach (var propInfo in from f in dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     where f.PropertyType.IsValueType || f.PropertyType == typeof(string)
                                     select f)
            {
                fields.Add(propInfo.Name, propInfo);
            }
        }

        public void NewCorrelationId()
        {
            Command.CorrelationId = Guid.NewGuid().ToString();
        }

        public int GetCommand()
        {
            return (int)(Command.Action == ActionType.Subscribe ? ActionType.Select : Command.Action);
        }

        public void SetCommand(int command)
        {
            var aType = (ActionType)command;
            Command.Action = aType == ActionType.Select && Notification ? ActionType.Subscribe : aType;
        }

        public string GetText()
        {
            return Command.Text;
        }

        public void SetText(string text)
        {
            Command.Text = text;
        }

        public int GetParamCount()
        {
            return Command.Params.Count;
        }

        public string GetParamName(int index)
        {
            return Command.Params.GetParameter(index).Name;
        }

        public object GetParam(string name)
        {
            return Command.Params[name];
        }

        public void SetParam(string name, object value)
        {
            Command.Params[name] = value;
        }

        public string GetFilter()
        {
            return Command.Filter;
        }

        public void SetFilter(string filter)
        {
            if ((Command.Filter ?? "") != (filter ?? ""))
            {
                Command.Filter = filter.Replace('\'', '"'); // TODO: !!! Change quoted symbol
                if (Active)
                    Connection.OpenCommand(this);
            }
        }

        public bool GetNotification()
        {
            return Notification;
        }

        public void SetNotification(bool notification)
        {
            if (Active && notification)
                throw new Exception("Can not change notification on active command");
            this.Notification = notification;
            if (Command.Action == ActionType.Select && notification)
                Command.Action = ActionType.Subscribe;
            else if (Command.Action == ActionType.Subscribe && !notification)
                Command.Action = ActionType.Select;
        }

        public object Data
        {
            get 
            {
                return enumerator == null ? _data : enumerator.Current;
            }
        }

        public void ReceiveCommand(DataSet dataSet)
        {
            UpdateData(dataSet.Command.Data);

            if (Active && receiveEvent != null)
                try
                {
                    receiveEvent(instanceId, this);
                }
                catch
                {
                    Connection.CloseCommand(this);
                }
        }

        public void OnReceive(ReceiveCommand receiveEvent)
        {
            this.receiveEvent += receiveEvent;
        }

        public void First()
        {
            if (_data is IEnumerable)
            {
                enumerator = ((IEnumerable)_data).GetEnumerator();
                _eof = !enumerator.MoveNext();
            }
            else
            {
                enumerator = null;
                _eof = true;
            }
        }

        public bool EOF()
        {
            return _eof;
        }

        public bool Next()
        {
            if (enumerator == null)
                throw new Exception(String.Format("Data is not supported IEnumerable: {0}", Data == null ? "<NULL>" : Data.GetType().Name));
            _eof = !enumerator.MoveNext();
            return !_eof;
        }

        public int Count
        {
            get
            {
                try
                {
                    if (_data == null)
                        return 0;
                    else if (_data.GetType().IsValueType || _data is string)
                        return 1;
                    else if (_data as IList != null)
                        return ((IList)_data).Count;
                    // TODO: ??? Count for IEnumerable
                    else
                        return -1;
                }
               catch (Exception ex)
               {
                    throw ex;
               }
            }
        }

        public int RecordCount()
        {
            return Count;
        }

        public string GetFieldNames()
        {
            //if (_data == null || _command.Fields.Count() > 0)
            //    return _command.Fields.ToString(); // TODO: !!!

            StringBuilder sb = new StringBuilder();
            foreach (var fieldName in Command.Fields ?? fields.Keys.ToArray())
            {
                if (sb.Length != 0)
                    sb.Append(";");
                sb.Append(fieldName);
            }
            return sb.ToString();
        }

        public void SetFieldNames(string fieldNames)
        {
            if (String.IsNullOrEmpty(fieldNames))
                Command.Fields = null;
            else
                Command.Fields = fieldNames.Split(',');
        }

        public int GetFieldType(string fieldName)
        {
            var propInfo = fields[fieldName];
            TypeCode codeType;

            if (propInfo.PropertyType == typeof(TimeSpan))
                codeType = TypeCode.DateTime;
            else
                codeType = Type.GetTypeCode(propInfo.PropertyType);

            switch (codeType)
            {
                // Variants
                case TypeCode.Int16: return 2;    // varSmallint
                case TypeCode.Int32: return 3;    // varInteger
                case TypeCode.Byte: return 2;     // varSmallint
                case TypeCode.Boolean: return 11; // varBoolean
                case TypeCode.Double: return 5;   // varDouble
                case TypeCode.String: return 256; // varString
                case TypeCode.Int64: return 20;   // varInt64

                // TODO: ???
                case TypeCode.Decimal: return 5;   // varDouble

                // Extended
                case TypeCode.DateTime: return 301;  // varExtDateTime

                default:
                    throw new Exception(String.Format(@"Unsupported field type '{0}' field '{1}'", codeType.ToString(), fieldName));
            }
        }

        public T GetFieldValue<T>(string fieldName)
        {
            try
            {
                var data = fields[fieldName].GetValue(Data, null);

                if (data == null)
                    return (T)data;
                else if (data.GetType() == typeof(decimal) && typeof(T) == typeof(double))
                    return (T)(object)decimal.ToDouble((decimal)data);
                else
                    return (T)data;
            }
            catch // (Exception ex)            
            {
                throw new Exception(String.Format("Invalid cast from {0} to {1}", fields[fieldName].GetValue(Data, null).GetType(), typeof(T).Name));
            }
        }

        public int GetFieldAsInteger(string fieldName)
        {
            return GetFieldValue<int>(fieldName);
        }

        public bool GetFieldAsBoolean(string fieldName)
        {
            return GetFieldValue<bool>(fieldName);
        }

        public double GetFieldAsDouble(string fieldName)
        {
            return GetFieldValue<double>(fieldName);
        }

        public string GetFieldAsString(string fieldName)
        {
            return GetFieldValue<string>(fieldName);
        }

        public long GetFieldAsLong(string fieldName)
        {
            return GetFieldValue<long>(fieldName);
        }

        public double GetFieldAsDateTime(string fieldName)
        {
            var value = GetFieldValue<object>(fieldName);
            if (value is DateTime)
                return ((DateTime)value).ToOADate();
            else if (value is TimeSpan)
                return new DateTime().Add((TimeSpan)value).ToOADate();
            else
                throw new Exception(String.Format("Invalid cast '{0}' to 'DateTime'", value.GetType().Name));
        }
    }
}