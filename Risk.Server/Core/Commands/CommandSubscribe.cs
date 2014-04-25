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
            if (Object == null)
                throw new Exception("Object name is empty");

            Object.AddNotification(Connection, CorrelationId, Parameters);

            SetResult(Object.GetData(Parameters));

            // Notify all data
            // NotifyChanges(notification, new NotificationData { NotificationType = NotificationType.Create, Data = GetData(parameters) });
        }
    }
}