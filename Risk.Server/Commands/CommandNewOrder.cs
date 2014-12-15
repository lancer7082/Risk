using System;
using System.Collections.Generic;
using System.Linq;

namespace Risk.Commands
{
    /// <summary>
    ///  Команда закрытия позиции, вызываемая с клиента
    /// </summary>
    [Command("NewOrder")]
    public class CommandNewOrder : CommandServer
    {
        protected internal override void InternalExecute()
        {
            if (Connection != null)
                Connection.CheckDealerUser();

            if (!Parameters.Contains("TradeCode"))
                throw new Exception("Не указан торговый код");

            if (!Parameters.Contains("SecCode"))
                throw new Exception("Не указан код инструмента");

            if (!Parameters.Contains("Quantity"))
                throw new Exception("Не указано кол-во");

            //// Если создано вручную с клиента, то считаем подтвержденным
            var order = new Order
            {
                TradeCode = Parameters["TradeCode"].ToString(),
                SecCode = Parameters["SecCode"].ToString(),
                Quantity = (int)Parameters["Quantity"],
                Price = (decimal)((double)Parameters["Price"]),
                OrderType = (OrderType)Parameters["OrderType"],
                Date = ServerBase.Current.ServerTime
            };

            if (String.IsNullOrWhiteSpace(order.TradeCode))
                throw new Exception("Не указан торговый код");

            ServerBase.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
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
