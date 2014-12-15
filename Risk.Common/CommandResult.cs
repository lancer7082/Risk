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
    [KnownType("KnownTypes")]
    public class CommandResult
    {
        [DataMember]
        public FieldInfo[] FieldsInfo { get; set; }

        [DataMember]
        public object Data { get; set; }

        public static IEnumerable<Type> KnownTypes()
        {
            return Command.KnownTypes();
        }
    }
}
