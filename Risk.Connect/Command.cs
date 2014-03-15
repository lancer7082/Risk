using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Risk
{
    public class Command : ICommand, IDisposable
    {
        private Dictionary<string, PropertyInfo> fields = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
        private IEnumerator enumerator;
        private bool _eof;
        private ReceiveEvent receiveEvent;
        private int instanceId;

        public Connection Connection { get; private set; }
        public string CorrelationId { get; private set; }
        public bool Active { get; internal set; }
        public string Text { get; set; }
        public Type ElementType { get; set; }
        public bool Notification { get; set; }

        public Command(Connection connection, int instanceId)
        {
            this.Connection = connection;
            this.instanceId = instanceId;
            
        }

        internal Command(Connection connection, string action, string correlationId, object data, Type elementType = null)
        {
            this.Connection = connection;
            this.CorrelationId = correlationId;
            this.ElementType = elementType;
            this.Data = data;
            this.Text = action;
        }

        public void Dispose()
        {
            Data = null;
            if (Active)
                Connection.CloseCommand(this);
            // TODO: ??? Thread.MemoryBarrier();
        }

        public void NewCorrelationId()
        {
            this.CorrelationId = Guid.NewGuid().ToString();
        }

        public string GetText()
        {
            return Text;
        }

        public void SetText(string text)
        {
            this.Text = text;
        }

        public byte GetNotification()
        {
            return Notification ? (byte)1 : (byte)0;
        }

        public void SetNotification(byte notification)
        {
            //if (this.Notification == notification)
            //    return;
            if (Active && notification == 1)
                throw new Exception("Can not change notification on active command");
            this.Notification = notification == 1;
        }

        private object _data;
        public object Data
        {
            get 
            {
                return enumerator == null ? _data : enumerator.Current; 
            }

            set 
            {
                // TODO: ??? Кешировать разбор полей для типа
                _data = value;
                fields.Clear();

                First();

                if (_data == null)
                    return;

                var dataType = ElementType;
                if (dataType == null)
                {
                    dataType = _data.GetType();
                    if (_data is IEnumerable)
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
        }

        public void ReceiveCommand(Command command)
        {
            if (Active && receiveEvent != null)
                try
                {
                    receiveEvent(instanceId, command);
                }
                catch
                {
                    Connection.CloseCommand(this);
                }
                finally
                {
                    command.Dispose();
                }
        }

        public void OnReceive(ReceiveEvent receiveEvent)
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
                if (_data as IList != null)
                    return ((IList)_data).Count;
                else
                    return -1;
            }
        }

        public int RecordCount()
        {
            return Count;
        }

        public string GetFieldNames()
        {
            if (_data == null)
                throw new Exception("Data is epmty");

            StringBuilder sb = new StringBuilder();
            foreach (var fieldName in fields.Keys)
            {
                if (sb.Length != 0)
                    sb.Append(";");
                sb.Append(fieldName);
            }
            return sb.ToString();
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
                // case TypeCode.Int64: return 20;   // varInt64

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
            catch (Exception ex)            
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