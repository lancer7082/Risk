using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class Notification
    {
        public string CorrelationId { get; set; }
        public DataObject DataObject { get; set; }
        // TODO: ConcurrentDictionary<object, filter (Linq where)>
    }
}
