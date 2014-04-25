using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class DataObjectAttribute : Attribute
    {
        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Только для чтения (нельзя изменить данные командами)
        /// </summary>
        public bool ReadOnly { get; set; }

        public DataObjectAttribute(string name)
        {
            this.Name = name;
        }
    }
}
