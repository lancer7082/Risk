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
    [Command("ClosePosition")]
    public class CommandClosePosition : CommandServer
    {
        protected internal override void InternalExecute()
        {
            Order order = new Order();

            order.TradeCode = Parameters["TradeCode"].ToString();
            order.SecСode = Parameters["SecCode"].ToString();
            order.Quantity = (int)Parameters["Quantity"];
            order.Price = (decimal)((double)Parameters["Price"]);
            order.OrderType = (OrderType)Parameters["OrderType"];

            Server.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
                (
                    new Command
                    {
                        CommandText = "ClosePosition",
                        Data = order
                    }
                );
        }
    }
}
