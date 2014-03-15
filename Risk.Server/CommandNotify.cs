using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class CommandNotify : CommandSystem
    {
        public string Message { get; set; }

        public override object Execute()
        {
            ExecuteAsync();
            return null;
        }
    }
}