using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public abstract class CommandTable : CommandServer
    {
        public ITable Table
        {
            get
            {
                var table = Object as ITable;
                if (table == null)
                    throw new Exception(String.Format("DataObject '{0}' has not supported '{1}' command", Object.Name, GetType().Name));
                return table;
            }
        }
    }
}
