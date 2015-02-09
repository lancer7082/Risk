using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using NLog;

namespace Risk.Commands
{
    /// <summary>
    /// Команда AutoMarginCallClose
    /// </summary>
    [Command("AutoMarginCallClose")]
    public class CommandAutoMarginCallClose : CommandServer
    {
        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            AutoMarginCallClose();
        }

        #endregion

        /// <summary>
        /// AutoMarginCallClose
        /// </summary>
        private void AutoMarginCallClose()
        {
            if (Connection != null)
                Connection.CheckDealerUser();

            if (Parameters["TradeCode"] == null)
                throw new Exception(String.Format("Ошибка  в параметрах"));

            // торговый код
            var tradeCode = Parameters["TradeCode"].ToString();

            if (IsPositionsAlreadyClosed(tradeCode))
                throw new Exception("Установлена задержка на данный тип операции " + tradeCode);
            try
            {
                var marginCalls = Server.AutoMarginCallInfos.Where(s => s.TradeCode == tradeCode).ToList();

                ClientOrders.CancelActiveClientOrders(tradeCode, 2000);

                foreach (var autoMarginCallInfo in marginCalls)
                {
                    var order = new Order
                    {
                        TradeCode = autoMarginCallInfo.TradeCode,
                        SecCode = autoMarginCallInfo.InstrumentCode,
                        Date = ServerBase.Current.ServerTime,
                        Quantity = (int)Math.Abs(autoMarginCallInfo.QuantityForClose),
                        Price = 0,
                        OrderType = autoMarginCallInfo.PositionBalance > 0 ? OrderType.Sell : OrderType.Buy,
                        MarginCall = true,
                    };

                    ServerBase.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
                        (
                            new Command
                            {
                                CommandText = "NewOrder",
                                Data = order
                            }
                        );
                }
                NotifyClient(tradeCode);
            }
            catch (Exception e)
            {
                LogManager.GetCurrentClassLogger().Error("{0} CommandAutoMarginCallClose exception: " + e, tradeCode);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tradeCode"></param>
        /// <returns></returns>
        private static bool IsPositionsAlreadyClosed(string tradeCode)
        {
            // если такой записи еще нет в поручениях
            var closedPositionsOrder = Server.Orders.Where(s => s.TradeCode == tradeCode && s.MarginCall).OrderByDescending(s => s.Date).FirstOrDefault();
            if (closedPositionsOrder == null)
                return false;

            // если уже прошло достаточно времени, то можно закрывать
            if ((DateTime.Now - closedPositionsOrder.Date).TotalSeconds >= Server.Settings.MarginCallClosingPositionsInterval)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Оповещение клиента
        /// </summary>
        /// <param name="tradeCode"></param>
        private static void NotifyClient(string tradeCode)
        {
            var portfolioRules = new List<PortfolioRule>
            {
                new PortfolioRule
                {
                    RuleType = RuleType.MaxPercentUtilMarginCallExceed,
                    Portfolio = Server.Portfolios.SingleOrDefault(s => s.TradeCode == tradeCode),
                    RuleTime = ServerBase.Current.ServerTime,
                }
            };

            new CommandMerge
            {
                Object = Server.PortfolioRules,
                Data = portfolioRules,
                KeyFields = "TradeCode,RuleType",
                Fields = "RuleTime",
            }.ExecuteAsync();
        }
    }

}
