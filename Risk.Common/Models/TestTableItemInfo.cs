using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [Serializable]
    public class TestTableItemInfo
    {
        public int Id1 { get; set; }
        public int Value1 { get; set; }
        public string Text1 { get; set; }
        public DateTime DateTime1 { get; set; }
        public TimeSpan Time1 { get; set; }
    }
}
