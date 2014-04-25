using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [Serializable]
    [DataContract]
    [KnownType(typeof(ServerObjectInfo))]
    [KnownType(typeof(HierarchyObject[]))]   
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