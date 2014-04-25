using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Выборка строк из таблицы
    /// </summary>
    [Command("Select")]
    public class CommandSelect : CommandTable
    {
        protected internal override void InternalExecute()
        {
            if (Object == null)
                throw new Exception("Object name is empty");

            SetResult(Object.GetData(Parameters));
        }
    }
}