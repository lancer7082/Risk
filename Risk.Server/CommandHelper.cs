using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public static class CommandHelper
    {
        public static object Execute(this Command command, Connection connection = null)
        {
            return (new CommandServer(connection, command)).Execute();
        }

        public static T Execute<T>(this Command command, Connection connection = null)
        {
            return (T)(new CommandServer(connection, command)).Execute();
        }
    }
}