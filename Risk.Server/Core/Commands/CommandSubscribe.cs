using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Подписка на обновление таблицы
    /// </summary>
    [Command("Subscribe")]
    public class CommandSubscribe : CommandSelect
    {
        protected internal override void InternalExecute()
        {
            Object.AddNotification(Connection, CorrelationId, Parameters);
            base.InternalExecute();            
        }
    }
}