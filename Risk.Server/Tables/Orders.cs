using System;
using System.Collections.Generic;

namespace Risk
{
    /// <summary>
    /// Поручения
    /// </summary>
    [Table("Orders", KeyFields = "OrderId")]
    public class Orders : Table<Order>
    {
        public override void TriggerAfter(TriggerCollection<Order> items)
        {
            lock (Server.Current.DataBase)
            {
                //  Сохранение поручения в БД
                foreach(var o in items.Updated) 
                {
                    int orderId = Server.Current.DataBase.SaveOrder(o);
                    o.OrderId = orderId;
                }
            }
        }
    }
}
