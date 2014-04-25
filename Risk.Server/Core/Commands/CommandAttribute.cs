using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// Имя команды
        /// </summary>
        public string CommandName { get; private set; }

        public CommandAttribute(string commandName)
        {
            this.CommandName = commandName;
        }
    }
}
