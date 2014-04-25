using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    ///  Команда закрытия позиций по счету, вызываемая с клиента
    /// </summary>
    [Command("ClosePositionsByAccount")]
    public class CommandClosePositionsByAccount : CommandServer
    {
        protected internal override void InternalExecute()
        {
            string TradeCode = Parameters["TradeCode"].ToString();

            if (String.IsNullOrWhiteSpace(TradeCode))
                throw new Exception("Не указан торговый код");

            var portfolio = Server.Portfolios.FirstOrDefault(x => x.TradeCode == TradeCode);
            if (portfolio == null)
                throw new Exception(String.Format("Не найден портфель клиента '{0}'", TradeCode));

            if (!portfolio.MarginCall)
                throw new Exception(String.Format("В портфеле не выставлен признак MarginCall '{0}'", TradeCode));

            var positions = from pos in Server.Positions
                            where pos.TradeCode == TradeCode &&
                                pos.Balance != 0    
                            select pos;

            var time = Server.Current.ServerTime;

            foreach (var pos in positions)
            {
                var order = new Order
                {
                    TradeCode = pos.TradeCode,
                    SecСode = pos.SecCode,
                    Date = time,
                    Quantity = Math.Abs(pos.Balance),
                    Price = 0,
                    OrderType = pos.Balance > 0 ? OrderType.Sell : OrderType.Buy,
                    MarginCall = true,
                };
                
                Server.Current.AddIns["Risk.Transaq.TransaqAddIn, Risk.Transaq"].Execute
                    (
                        new Command
                        {
                            CommandText = "NewOrder",
                            Data = order
                        }
                    );
            }

            // Отправка оповещения о закрытии позиций пользователю и клиенту
            new CommandInsert
            {
                Object = Server.Alerts,
                Data = new Alert
                {
                    DateTime = time,
                    Portfolio = portfolio,
                    NotifyType = Server.Settings.NotifyTypeMaxPercentUtilMarginCall,
                    Text = String.Format(@"В соответствии с регламентом проведения
торгов {0} все открытые позиции по счету {1} принудительно закрыты в связи с 
недостаточным обеспечением. % использования капитала = {2}", time.ToLongDateString(), 
                        portfolio.TradeCode, Math.Round(portfolio.UtilizationFact, 2)),
                },
            };
  
        }
    }
}
