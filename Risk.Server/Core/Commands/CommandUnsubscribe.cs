using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk.Commands
{
    /// <summary>
    /// Отписка от обновления таблицы
    /// </summary>
    [Command("Unsubscribe")]
    public class CommandUnsubscribe : CommandServer
    {
        protected internal override void InternalExecute()
        {
            if (Object == null)
                throw new Exception("Object name is empty");

            Object.RemoveNotification(Connection, CorrelationId);
        }
    }
}
