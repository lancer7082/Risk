using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Risk
{
    [Serializable]
    [DataContract]
    public class HierarchyObject
    {
        private HierarchyObject _parent;
        
        [DataMember]
        private HierarchyObject[] _items;
       
        [DataMember]
        public string Name { get; set; }

        public HierarchyObject Parent 
        {
            get { return _parent; }
            set 
            {
                if (_parent == value)
                    return;
                if (_parent != null)
                {
                    var listItems = new List<HierarchyObject>(_parent._items);
                    listItems.Remove(this);
                    _parent._items = listItems.ToArray();
                }
                _parent = value;
                if (_parent != null)
                {
                    var listItems = new List<HierarchyObject>(_parent._items ?? new HierarchyObject[] {});
                    listItems.Add(this);
                    _parent._items = listItems.ToArray();
                }
            }
        }

        public HierarchyObject[] Items 
        {
            get { return _items; }
        }

        [DataMember]
        public object Data { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}