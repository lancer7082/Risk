using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    ///  Команда закрытия позиции, вызываемая с клиента
    /// </summary>
    [Command("NewOrder")]
    public class CommandNewOrder : CommandServer
    {
        protected internal override void InternalExecute()
        {
            if (!Parameters.Contains("TradeCode"))
                throw new Exception("Не указан торговый код");

            if (!Parameters.Contains("SecCode"))
                throw new Exception("Не указан код инструмента");

            if (!Parameters.Contains("Quantity"))
                throw new Exception("Не указано кол-во");

            Order order = new Order();
            order.TradeCode = Parameters["TradeCode"].ToString();
            order.SecСode = Parameters["SecCode"].ToString();
            order.Quantity = (int)Parameters["Quantity"];
            order.Price = (decimal)((double)Parameters["Price"]);
            order.OrderType = (OrderType)Parameters["OrderType"];
            //// Если создано вруную с клиента,
            //// то считаем подтвержденным
            //order.OrderStatus = OrderStatus.Approved;
            order.Date = Server.Current.ServerTime;

            if (String.IsNullOrWhiteSpace(order.TradeCode))
                throw new Exception("Не указан торговый код");

            Server.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
                (
                    new Command
                    {
                        CommandText = "NewOrder",
                        Data = order
                    }
                );
        }
    }
}
