using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public enum CommandType
    {
        Select = 0,
        Create = 1,
        Update = 2,
        Delete = 3,
        Synchronize = 4,
        Subscribe = 100,
        Unsubscribe = 101
    }
}
