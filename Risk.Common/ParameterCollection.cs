using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Risk
{
    /// <summary>
    /// Коллекция параметров для комманды
    /// </summary>
    [Serializable]
    public class ParameterCollection : IList, ICollection, IEnumerable
    {
        private static Type ItemType = typeof(Parameter);

        private List<Parameter> _items;
        private List<Parameter> Items
        {
            get
            {
                if (_items == null)
                    _items = new List<Parameter>();
                return _items;
            }
        }

        public Parameter GetParameter(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index < 0)
                throw new Exception(String.Format("Paramenter '{0}' not found", parameterName));
            else
                return Items[index];
        }

        public void SetParameter(string parameterName, Parameter parameter)
        {
            var index = IndexOf(parameterName);
            if (index < 0)
                Add(parameter);
            else
                Items[index] = parameter;
        }

        public Parameter GetParameter(int index)
        {
            RangeCheck(index);
            return Items[index];
        }

        public void SetParameter(int index, Parameter parameter)
        {
            RangeCheck(index);
            Items[index] = parameter;
        }

        private void RangeCheck(int index)
        {
            if (index < 0 || Count <= index)
                throw new Exception(String.Format("The index must be greater than zero but less than Count = {0}", Count));
        }

        public Parameter Add()
        {
            var parameter = new Parameter();
            Items.Add(parameter);
            return parameter;
        }

        public int Add(object value)
        {
            ValidateType(value);
            Items.Add((Parameter)value);
            return (this.Count - 1);
        }

        public void Clear()
        {
            if (_items != null)
                Items.Clear();
        }

        public bool Contains(Parameter value)
        {
            return (-1 != IndexOf(value));
        }

        public bool Contains(string parameterName)
        {
            return (-1 != IndexOf(parameterName));
        }

        public bool Contains(object value)
        {
            return (-1 != IndexOf(value));
        }

        public int IndexOf(object value)
        {
            if (value != null)
            {
                this.ValidateType(value);
                if (_items != null)
                {
                    int count = Items.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (value == Items[i])
                            return i;
                    }
                }
            }
            return -1;
        }

        public int IndexOf(string parameterName)
        {
            if (_items != null)
            {
                int num = 0;
                foreach (Parameter parameter in Items)
                {
                    if (String.Equals(parameter.Name, parameterName))
                        return num;
                    num++;
                }
            }
            return -1;
        }

        public void Insert(int index, Parameter value)
        {
            this.Insert(index, value);
        }

        public void Insert(int index, object value)
        {
            ValidateType(value);
            Items.Insert(index, (Parameter)value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            ValidateType(value);
            Items.Remove((Parameter)value);
        }

        public void RemoveAt(int index)
        {
            Items.RemoveAt(index);
        }

        public object this[int index]
        {
            get
            {
                return GetParameter(index).Value;
            }
            set
            {
                GetParameter(index).Value = value;
            }
        }

        public object this[string parameterName, object defaultValue]
        {
            get
            {
                return Contains(parameterName) ? GetParameter(parameterName).Value : defaultValue;
            }
        }

        public object this[string parameterName]
        {
            get
            {
                return this[parameterName, null];
            }
            set
            {
                var index = IndexOf(parameterName);
                if (index < 0)
                {
                    if (value != null)
                        SetParameter(parameterName, new Parameter { Name = parameterName, Value = value });
                }
                else if (value == null)
                    RemoveAt(index);
                else
                    Items[index].Value = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            Items.CopyTo(array.Cast<Parameter>().ToArray(), index); // TODO: ???
        }

        public int Count
        {
            get { return _items == null ? 0 : Items.Count; }
        }

        public bool IsSynchronized
        {
            get { return true; }
        }

        public object SyncRoot
        {
            get { return ((ICollection)Items).SyncRoot; }
        }

        public IEnumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        private void ValidateType(object value)
        {
            if (value == null)
                throw new Exception("Parameter can not be null");
            else if (!ItemType.IsInstanceOfType(value))
                throw new Exception(String.Format("Parameter must be type '{0}' actual '{1}'", ItemType.Name, value.GetType().Name));
        }
    }
}