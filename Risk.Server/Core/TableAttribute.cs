using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class TableAttribute : DataObjectAttribute
    {
        /// <summary>
        /// Первичный ключ таблицы, может состоять из нескольких полей (указываются через запятую), если не указан, то используется ссылка на объект
        /// </summary>
        public string KeyFields { get; set; }

        public TableAttribute(string tableName)
            : base(tableName)
        {
        }

        // TODO: ??? [TableKey("AccountId")]
        // TODO: ??? [TableIndex("TradeCode", TableIndexType.Unique)]
        // TODO: ??? [TableIndex("Name", TableIndexType.NonUnique)]
    }
}
