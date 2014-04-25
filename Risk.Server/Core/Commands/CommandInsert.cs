using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Вставка строк в таблицу
    /// </summary>
    [Command("Insert")]
    public class CommandInsert : CommandTable
    {
        protected internal override void InternalExecute()
        {
            if (Data == null)
                Data = Table.CreateDataFromParams(Parameters, false);
            Table.Insert(Data);
        }
    }
}