using System;
using System.Collections.Generic;

namespace Risk
{
    /// <summary>
    /// Таблица клиентов
    /// </summary>
    [Table("Clients", KeyFields = "Id")]
    public class Clients : Table<Client>
    {
        protected override void NotifyChanges(Notification notification, NotificationData notificationData)
        {
            foreach (var item in (IEnumerable<Client>)notificationData.Data)
            {
                item.UpdateTime = Server.Current.ServerTime.TimeOfDay;
            }
            base.NotifyChanges(notification, notificationData);
        }
    }
}
