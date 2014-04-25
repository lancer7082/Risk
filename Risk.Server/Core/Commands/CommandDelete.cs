using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Удаление строк из таблицы
    /// </summary>
    [Command("Delete")]
    public class CommandDelete : CommandTable
    {
        protected internal override void InternalExecute()
        {
            if (Data == null)
                Data = Table.CreateDataFromParams(Parameters, false);
            Table.Delete(Data);
        }
    }
}