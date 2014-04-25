using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class Table<T, TResult> : Table<T>
        where T : class, new()
        where TResult : class, new()
    {
        public override object GetData(ParameterCollection parameters)
        {
            IEnumerable<T> items = this;
            var predicate = Predicate(parameters);
            if (predicate != null)
                items = this.Where(predicate.Compile());

            return items.Select(x => x.ConvertType<T, TResult>()).ToArray();
        }

        protected override void NotifyChanges(Notification notification, NotificationData notificationData)
        {
            if (notificationData.Data is IEnumerable<T>)
            {
                IEnumerable<T> items = (IEnumerable<T>)notificationData.Data;
                if (notification.Predicate != null)
                    items = items.Where((Func<T, bool>)notification.Predicate);
                notificationData.Data = items.Select(x => x.ConvertType<T, TResult>()).ToArray();
            }
            base.NotifyChanges(notification, notificationData);
        }
    }
}
