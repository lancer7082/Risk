using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [DataContract]
    public class TestTableItem
    {
        [DataMember]
        public int Id { get; set; }
        
        [DataMember]
        public int Value { get; set; }
        public string Text { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan Time { get; set; }


        public static implicit operator TestTableItemInfo(TestTableItem item)
        {
            return new TestTableItemInfo
            {
                Id1 = item.Id,
                Value1 = item.Value,
                Text1 = item.Text + " (Info)",
                Time1 = item.Time,
            };
        }
    }
}