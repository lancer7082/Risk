using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class ExportData : IExportData
    {
        public Connection Connection { get; private set; }
        private Dictionary<string, PropertyInfo> fields = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
        private object _data;
        private IEnumerator enumerator;
        private bool _eof;

        public string FieldNames { get; set; }

        public object Data
        {
            get
            {
                return enumerator == null ? _data : enumerator.Current;
            }
        }

        public ExportData(Connection connection)
        {
            Connection = connection;
        }

        public void UpdateData(object data)
        {
            bool updateFields = true;
            if (_data != null && data != null)
                updateFields = _data.GetType() != data.GetType();

            this._data = data;

            First();

            if (updateFields)
            lock (fields)
            {
                fields.Clear();

                if (data == null || data.GetType().IsValueType || data is string)
                    return;

                Type dataType = null; // TODO: ??? Get from command

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
                                         select f)
                {
                    fields.Add(propInfo.Name, propInfo);
                }
            }
        }

        public void First()
        {
            // Connection.Trace("Data First");

            if (_data is IEnumerable)
            {
                enumerator = ((IEnumerable)_data).GetEnumerator();
                _eof = !enumerator.MoveNext();
            }
            else
            {
                enumerator = null;
                _eof = false;
            }
        }

        public bool EOF()
        {
            //if (_eof)
            //    Connection.Trace("Data EOF");
            return _eof;
        }

        public bool Next()
        {
            if (enumerator != null)
                _eof = !enumerator.MoveNext();
            else
                _eof = true;
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
                    else if (_data is IEnumerable)
                        return (_data as IEnumerable).Cast<object>().Count();
                    else
                        return 1;
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
            return FieldNames ?? string.Join(";", fields.Keys);
        }

        public void SetFieldNames(string fieldNames)
        {
            FieldNames = fieldNames;
        }

        public int GetFieldType(string fieldName)
        {
            if (!fields.ContainsKey(fieldName))
                throw new Exception(String.Format("Field '{0}' not found", fieldName));

            var propInfo = fields[fieldName];
            TypeCode codeType;

            if (propInfo.PropertyType == typeof(TimeSpan))
                codeType = TypeCode.DateTime;
            else
                codeType = Type.GetTypeCode(propInfo.PropertyType);

            switch (codeType)
            {
                // Variants
                case TypeCode.Int16:    return 0x0002;  // varSmallint
                case TypeCode.Int32:    return 0x0003;  // varInteger

                case TypeCode.Double:   return 0x0005;  // varDouble
                case TypeCode.Decimal:  return 0x0005;  // varDouble // TODO: ???
                case TypeCode.Boolean:  return 0x000B;  // varBoolean
                case TypeCode.Byte:     return 0x0011;  // varByte  /* TODO: ??? 2 */;     // varSmallint
                case TypeCode.UInt16:   return 0x0012;  // varWord
                case TypeCode.UInt32:   return 0x0013;  // varLongWord               
                case TypeCode.Int64:    return 0x0014;  // varInt64
                case TypeCode.UInt64:   return 0x0015;  // varUInt64

                case TypeCode.String:   return 0x0100;  // varString // TODO: ??? 0x0102 varUString

                // Extended
                case TypeCode.Object:   return 0x300;   // varExtObject
                case TypeCode.DateTime: return 0x301;   // varExtDateTime

                default:
                    throw new Exception(String.Format(@"Unsupported field type '{0}' field '{1}'", codeType.ToString(), fieldName));
            }
        }

        public object GetFieldValue(string fieldName)
        {
            var data = fields[fieldName].GetValue(Data, null);

            if (data == null)
                return data;
            else if (data.GetType() == typeof(decimal))
                return (object)decimal.ToDouble((decimal)data);
            else
                return data;
        }

        public IExportData GetFieldAsObject(string fieldName)
        {
            var value = GetFieldValue(fieldName);

            if (value == null)
                return null;

            var result = new ExportData(Connection);
            result.UpdateData(value);
            return result;
        }

        public double GetFieldAsDateTime(string fieldName)
        {
            var value = GetFieldValue(fieldName);
            if (value is DateTime)
                return ((DateTime)value).ToOADate();
            else if (value is TimeSpan)
                return new DateTime().Add((TimeSpan)value).ToOADate();
            else
                throw new Exception(String.Format("Invalid cast '{0}' to 'DateTime'", value.GetType().Name));
        }
    }
}
