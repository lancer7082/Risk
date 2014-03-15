using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [Serializable]
    public class ConnectionInfo
    {
        public string UserName { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public DateTime StartTime { get; set; }
        public string Id { get; set; }
    }
}