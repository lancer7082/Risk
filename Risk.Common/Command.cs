using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Risk
{
    [Serializable]
    [DataContract]
    [KnownType(typeof(Client[]))]
    [KnownType(typeof(Portfolio[]))]
    [KnownType(typeof(ConnectionInfo[]))]
    public class Command
    {
        private IEnumerable<string> _fields;
        
        [DataMember]
        public CommandType Type { get; set; }
        
        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public string CorrelationId { get; set; }

        [DataMember]
        public virtual IEnumerable<string> Fields
        {
            get 
            {
                return _fields ?? GetDefaultFields();
            }
            set 
            { 
                if (value == null || value.Count() == 0)
                    _fields = null;
                else
                    _fields = new List<string>(value);
            }
        }

        [DataMember]
        public virtual object Data { get; set; }

        public Command()
        {
            Type = Risk.CommandType.Select;
        }

        protected virtual IEnumerable<string> GetDefaultFields()
        {
            return Enumerable.Empty<string>();
        }
    }
}