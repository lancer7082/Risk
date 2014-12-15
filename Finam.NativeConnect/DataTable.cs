using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Finam.NativeConnect
{
    public class DataTable<T> : IDataTable
    {
        private Dictionary<ExportFieldInfo, PropertyInfo> fields = new Dictionary<ExportFieldInfo, PropertyInfo>();
        List<T> _items = new List<T>();
        int _maxBookmark = 0;
        List<int> _bookmarks = null;

        public string KeyFieldNames { get; private set; }
        protected PropertyInfo[] KeyFieldProperties { get; private set; }
        protected PropertyInfo[] UpdateProperties { get; private set; }
        protected Func<T, object> KeySelector { get; private set; }
        // TODO: ??? public string FieldNames { get; set; }

        private void CreateBookmarks()
        {
            _bookmarks = new List<int>(from i in _items select _items.IndexOf(i));
            _maxBookmark = _bookmarks.Count;
        }

        private void ClearBookmarks()
        {
            _bookmarks = null;
            _maxBookmark = 0;
        }

        public int RecordCount()
        {
            return _items.Count;
        }

        private int UpdateItem(T inserted, T deleted)
        {
            foreach (var prop in UpdateProperties)
            {
                var newValue = prop.GetValue(inserted, null);
                prop.SetValue(deleted, newValue, null);
            }
            return _items.IndexOf(deleted);
        }

        public int[] UpdateData(DataUpdateType dataUpdateType, Risk.FieldInfo[] fieldsInfo, IEnumerable items)
        {
            List<int> updatedIndexes = null;
            var updatedItems = items.Cast<T>();

            if (dataUpdateType != DataUpdateType.Create && KeySelector == null)
                throw new Exception("Key fields is null");

            switch (dataUpdateType)
            {
                case DataUpdateType.Create:
                    UpdateFields(fieldsInfo);
                    _items = new List<T>(updatedItems);
                    ClearBookmarks();
                    break;

                case DataUpdateType.Insert:
                    if (_items == null)
                    {
                        _items = new List<T>(updatedItems);
                        ClearBookmarks();
                    }
                    else
                    {
                        var insertedItems = updatedItems.GroupJoin(_items, KeySelector, KeySelector, (i, g) => g // left join
                            .Select(d => new { Inserted = i, Deleted = d, Insert = false })
                            .DefaultIfEmpty(new { Inserted = i, Deleted = i, Insert = true }))
                            .SelectMany(x => x);

                        updatedIndexes = new List<int>();
                        foreach (var item in insertedItems)
                        {
                            if (item.Insert)
                            {
                                updatedIndexes.Add(_items.Count);
                                _items.Add(item.Inserted);
                                if (_bookmarks != null)
                                {
                                    while (_bookmarks.Contains(_maxBookmark) && _maxBookmark < int.MaxValue) _maxBookmark++;
                                    if (_maxBookmark == int.MaxValue) _maxBookmark = 0;
                                    while (_bookmarks.Contains(_maxBookmark) && _maxBookmark < int.MaxValue) _maxBookmark++;
                                    if (_maxBookmark == int.MaxValue)
                                        _bookmarks.Add(-1); // Error: Bookmark out of range
                                    else
                                        _bookmarks.Add(_maxBookmark);
                                }
                            }
                            else
                                updatedIndexes.Add(UpdateItem(item.Inserted, item.Deleted));
                        }
                    }
                    break;

                case DataUpdateType.Update:
                    updatedIndexes = new List<int>();
                    var updated = updatedItems.Join(_items, KeySelector, KeySelector, (i, d) => new { Inserted = i, Deleted = d });
                    foreach (var instancePair in updated)
                         updatedIndexes.Add(UpdateItem(instancePair.Inserted, instancePair.Deleted));
                    break;

                case DataUpdateType.Delete:
                    updatedIndexes = new List<int>();
                    var deletedIndexes = updatedItems.Join(_items, KeySelector, KeySelector, (i, d) => _items.IndexOf(d));
                    foreach (var index in deletedIndexes)
                    {
                        if (_bookmarks == null)
                            CreateBookmarks();
                        _items.RemoveAt(index);
                        _bookmarks.RemoveAt(index);
                        updatedIndexes.Add(index);
                    }
                    updatedIndexes.Sort(); // must be sorted for delete
                    break;
            }
            if (updatedIndexes == null)
                return null;
            else
                return updatedIndexes.ToArray();
        }

        private IEnumerable<T> PrepareData(object data)
        {
            if (data == null)
                return null;

            if (data is IEnumerable)
                return ((IEnumerable)data).Cast<T>();
            else
            {
                var result = new List<T>();
                result.Add((T)data);
                return result;
            }
        }

        private void UpdateFields(Risk.FieldInfo[] fieldsInfo)
        {
            lock (fields)
            {
                fields.Clear();

                var dataType = typeof(T);

                if (dataType.IsValueType || dataType == typeof(string))
                {
                    fields.Add(new ExportFieldInfo
                    {
                        FieldName = "Column1",
                        FieldType = GetFieldType(Risk.FieldInfo.GetFieldTypeCode(dataType))
                    }, null);
                    return;
                }

                var ienum = dataType.GetInterface(typeof(IEnumerable<>).Name);
                if (ienum != null)
                    dataType = ienum.GetGenericArguments()[0];

                string keyFieldNames = null;
                var keyFieldProperties = new List<PropertyInfo>();
                var updateProperties = new List<PropertyInfo>();

                if (fieldsInfo == null)
                    foreach (var propInfo in from f in dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public) select f)
                    {
                        fields.Add(new ExportFieldInfo
                        {
                            FieldName = propInfo.Name,
                            FieldType = GetFieldType(Risk.FieldInfo.GetFieldTypeCode(propInfo.PropertyType))
                        }, propInfo);
                    }
                else
                {
                    foreach (var fieldInfo in fieldsInfo)
                    {
                        var property = dataType.GetProperty(fieldInfo.FieldName, BindingFlags.Instance | BindingFlags.Public);
                        if (property == null)
                            continue; // TODO: ??? Error

                        fields.Add(new ExportFieldInfo
                        {
                            FieldName = fieldInfo.FieldName,
                            FieldType = GetFieldType(fieldInfo.DataType),
                        }, property);

                        if (fieldInfo.IsKey)
                        {
                            keyFieldProperties.Add(property);
                            if (!String.IsNullOrWhiteSpace(keyFieldNames)) keyFieldNames += ",";
                            keyFieldNames += fieldInfo.FieldName;
                        }
                        else if (!fieldInfo.ReadOnly)
                            updateProperties.Add(property);
                    }
                }

                if (!String.IsNullOrWhiteSpace(keyFieldNames))
                {
                    KeyFieldNames = keyFieldNames;
                    KeyFieldProperties = keyFieldProperties.ToArray();
                    UpdateProperties = updateProperties.ToArray();
                    KeySelector = CreateSelectorExpression(KeyFieldProperties);
                }
                else
                    KeySelector = null;
            }
        }

        public int FieldCount()
        {
            return fields.Count;
        }

        public ExportFieldInfo GetField(int fieldIndex)
        {
            return fields.ElementAt(fieldIndex).Key;
        }

        public int GetRecordIndex(int bookmark)
        {
            return _bookmarks == null ? bookmark : _bookmarks.IndexOf(bookmark);
        }

        public object[] GetRecordData(int recordIndex)
        {
            var result = new object[fields.Count + 1];
            int i = 0;
            result[i++] = _bookmarks == null ? recordIndex : _bookmarks[recordIndex];
            foreach (var prop in fields.Values)
            {
                object value = prop == null ? _items[recordIndex] : prop.GetValue(_items[recordIndex], null);
                if (value != null)
                {
                    if (value.GetType() == typeof(TimeSpan))
                        value = new DateTime() + (TimeSpan)value;
                    else if (Type.GetTypeCode(value.GetType()) == TypeCode.Object)
                        value = (byte)0;
                    else if (value.GetType() == typeof(decimal))
                        value = (object)decimal.ToDouble((decimal)value);
                }
                // !!! Only for debug bokkmarks
                // if (prop.Name == "Text") value = (string)value + String.Format(" ({0})", result[0]);
                result[i++] = value;
            }
            return result;
        }

        public object GetFieldData(int fieldIndex, int recordIndex)
        {
            return fields.ElementAt(fieldIndex).Value.GetValue(_items[recordIndex], null);
        }

        private Func<T, object> CreateSelectorExpression(PropertyInfo[] properties)
        {
            if (properties == null || properties.Length == 0)
                return x => x;

            if (properties.Count() == 1)
            {
                var param = Expression.Parameter(typeof(T), "x");
                var propValues = Expression.Property(param, properties[0]);
                var selector = Expression.Lambda<Func<T, object>>(Expression.TypeAs(propValues, typeof(object)), param);
                return selector.Compile();
            }
            else
            {
                var param = Expression.Parameter(typeof(T), "x");
                var paramExpression = new List<Expression>();
                var paramTypes = new List<Type>();
                foreach (var property in properties)
                {
                    paramExpression.Add(Expression.Property(param, property));
                    paramTypes.Add(property.PropertyType);
                }
                var selector = Expression.Lambda<Func<T, object>>(Expression.Call(typeof(Tuple), "Create", paramTypes.ToArray(), paramExpression.ToArray()), param);
                return selector.Compile();
            }
        }

        public ExportFieldType GetFieldType(TypeCode codeType)
        {
            switch (codeType)
            {
                // Variants
                case TypeCode.Int16: return ExportFieldType.ftSmallint;
                case TypeCode.Int32: return ExportFieldType.ftInteger;
                case TypeCode.Int64: return ExportFieldType.ftLargeint;

                case TypeCode.Double: return ExportFieldType.ftFloat;
                case TypeCode.Decimal: return ExportFieldType.ftFloat;
                case TypeCode.Boolean: return ExportFieldType.ftBoolean;

                case TypeCode.Byte: return ExportFieldType.ftByte;
                case TypeCode.UInt16: return ExportFieldType.ftWord;
                case TypeCode.UInt32: return ExportFieldType.ftLongWord;
                // TODO: ??? case TypeCode.UInt64: 

                case TypeCode.DateTime: return ExportFieldType.ftDateTime;

                case TypeCode.String: return ExportFieldType.ftWideString;

                case TypeCode.Object: return ExportFieldType.ftDataSet;
                    	        
                // Extended
                // TODO: ???  case TypeCode.Object: return 0x300;   // varExtObject
                // TODO: ??? case TypeCode.DateTime: return 0x301;   // varExtDateTime

                default: return 0x00; // TODO: ??? throw new Exception(String.Format(@"Unsupported field type '{0}' field '{1}'", codeType.ToString(), propInfo.Name));
            }
        }
    }
}