using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [DataContract]
    public struct FieldInfo
    {
        [DataMember]
        public string FieldName { get; set; }
        
        [DataMember]
        public TypeCode DataType { get; set; }

        [DataMember]
        public bool IsKey { get; set; }

        [DataMember]
        public int Size { get; set; }

        [DataMember]
        public string Caption { get; set; }

        [DataMember]
        public bool ReadOnly { get; set; }

        [DataMember]
        public bool Hidden { get; set; }

        public static TypeCode GetFieldTypeCode(Type type)
        {
            var codeType = Type.GetTypeCode(type);
            if (type == typeof(TimeSpan))
                return TypeCode.DateTime;
            else if (type == typeof(Guid))
                return TypeCode.String;
            else
                return codeType;
        }
    }
}