using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;

namespace Risk
{
    class ClientOrders
    {

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Обработка активный заявок клиента
        /// </summary>
        /// <param name="tradeCode"></param>
        /// <param name="waitMilliseconds"></param>
        public static void CancelActiveClientOrders(string tradeCode, int waitMilliseconds = 0)
        {
            try
            {
                var activeOrders = GetActiveClientOrders(tradeCode);

                if (!activeOrders.Any())
                    return;

                CancelActiveClientOrders(tradeCode, activeOrders);

                if (waitMilliseconds <= 0)
                    return;

                activeOrders = GetActiveClientOrders(tradeCode);

                if (!activeOrders.Any())
                    return;

                Thread.Sleep(waitMilliseconds);
            }
            catch (Exception e)
            {
                Log.Error("CancelActiveClientOrdersException: " + e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tradeCode"></param>
        /// <param name="activeOrders"></param>
        private static void CancelActiveClientOrders(string tradeCode, IEnumerable<int> activeOrders)
        {
            foreach (var orderId in activeOrders)
            {
                var cmdCancel = new Command
                {
                    CommandText = "CancelOrder",
                    Parameters = new ParameterCollection
                    {
                        new Parameter
                        {
                            Name = "OrderId",
                            Value = orderId
                        },
                        new Parameter
                        {
                            Name = "TradeCode",
                            Value = tradeCode
                        }
                    }
                };

                ServerBase.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
                    (
                        cmdCancel
                    );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tradeCode"></param>
        /// <returns></returns>
        private static List<int> GetActiveClientOrders(string tradeCode)
        {
            var cmd = new Command
            {
                CommandText = "GetActiveClientOrders",
                Data = tradeCode
            };

            var activeOrders = ServerBase.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
                (
                    cmd
                ) as List<int>;
            return activeOrders;
        }
    }
}
