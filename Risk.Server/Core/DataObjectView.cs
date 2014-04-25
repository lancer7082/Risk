using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risk
{
    public class DataObjectView<T> : DataObjectBase<T>
        where T : class, new()
    {
        private T _data;
        private T _cacheData;

        public T Data { get { return _data; } }

        public DataObjectView(T data)
        {
            this._data = data;
        }

        public override object GetData(ParameterCollection parameters)
        {
            if (_cacheData == null)
                lock (_data)
                {
                    var newCache = _data.CloneObject();
                    Interlocked.Exchange(ref _cacheData, newCache);
                }
            return _cacheData;
        }

        public override void SetData(ParameterCollection parameters, object data)
        {
            var properties = GetProperties((string)parameters["Fields"]);
            lock (_data)
            {
                foreach (var property in properties)
                    property.SetValue(_data, property.GetValue(data));
                Interlocked.Exchange(ref _cacheData, null);
            }
        }
    }
}