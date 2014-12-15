using System;
using System.Collections;
using System.Collections.Generic;
using Risk;

namespace Finam.NativeConnect
{
    public class ExportRecordset : IExportRecordset
    {
        private static Hashtable dataTableTypes = Hashtable.Synchronized(new Hashtable());
        private Type _itemType;
        private IDataTable _data;
        private ExportParameters _exportParameters;

        protected int InstanceId { get; private set; }

        public Connection Connection { get; private set; }
        public ParameterCollection Parameters { get; private set; }

        public IExportParameters GetParameters()
        {
            if (_exportParameters == null)
                _exportParameters = new ExportParameters(Parameters);
            return _exportParameters;
        }

        public ExportRecordset(Connection connection, int instanceId)
        {
            this.Parameters = new ParameterCollection();
            this.Connection = connection;
            this.InstanceId = instanceId;
        }

        public int RecordCount()
        {
            return _data == null ? 0 : _data.RecordCount();
        }

        public int GetRecordIndex(int bookmark)
        {
            if (_data == null)
                throw new Exception("Data is empty");
            return _data.GetRecordIndex(bookmark);
        }

        public object[] GetRecordData(int recordIndex)
        {
            if (_data == null)
                throw new Exception("Data is empty");
            return _data.GetRecordData(recordIndex);
        }

        public int FieldCount()
        {
            return _data == null ? 0 : _data.FieldCount();
        }

        public ExportFieldInfo GetField(int index)
        {
            if (_data == null)
                throw new Exception("Data is empty");
            return _data.GetField(index);
        }

        public IExportRecordset GetFieldAsData(int fieldIndex, int recordIndex)
        {
            if (_data == null)
                throw new Exception("Data is empty");

            // TODO: ???
            // if (recordIndex == _items.Count) // BUGFIX: Get fields on EOF
            //    return new ExportDataTable(Connection, _dataType);

            var result = new ExportRecordset(Connection, 0);
            var value = _data.GetFieldData(fieldIndex, recordIndex);
            result.UpdateData(DataUpdateType.Create, null, value);
            return result;
        }

        public int[] UpdateData(DataUpdateType dataUpdateType, FieldInfo[] fieldsInfo, object data)
        {
            if (dataUpdateType == DataUpdateType.Create)
            {
                if (data == null)
                {
                    _data = null;
                    return null;
                }

                var newItemType = GetItemType(data.GetType());
                if (_data == null || _itemType != newItemType)
                {
                    _itemType = newItemType;
                    var dataTableType = GetDataTableType(GetItemType(data.GetType()));
                    _data = (IDataTable)Activator.CreateInstance(dataTableType);
                }
            }
            if (_data != null)
                return _data.UpdateData(dataUpdateType, fieldsInfo, PrepareData(data));
            else
                return null;
        }

        private IEnumerable PrepareData(object data)
        {
            if (data == null)
                return null;

            if (data is IEnumerable)
                return (IEnumerable)data;
            else
            {
                var items = new ArrayList();
                items.Add(data);
                return items;
            }
        }

        protected Type GetItemType(Type dataType)
        {
            //if (dataType == null || dataType.IsValueType || dataType == typeof(string))
            //    throw new Exception("Error data type");

            var ienum = dataType.GetInterface(typeof(IEnumerable<>).Name);
            if (ienum != null)
                dataType = ienum.GetGenericArguments()[0];

            return dataType;
        }

        protected Type GetDataTableType(Type itemType)
        {
            string key = itemType.ToString();
            if (dataTableTypes.Contains(key))
                return (Type)dataTableTypes[key];

            Type constructedType = typeof(DataTable<>).MakeGenericType(itemType);
            dataTableTypes.Add(key, constructedType);

            return constructedType;
        }
    }
}
