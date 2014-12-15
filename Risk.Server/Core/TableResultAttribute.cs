using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class TableResultAttribute : TableAttribute
    {
        /// <summary>
        /// Первичный ключ таблицы для возвращаемых данных, может состоять из нескольких полей (указываются через запятую)
        /// </summary>
        public string ResultKeyFields { get; set; }

        public TableResultAttribute(string tableName)
            : base(tableName)
        {
        }
    }
}
