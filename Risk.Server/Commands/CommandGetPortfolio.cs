using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Risk.Commands
{
    /// <summary>
    /// Загрузка портфеля
    /// </summary>
    [Command("GetPortfolio")]
    public class CommandGetPortfolio : CommandServer
    {
        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            GetPortfolio();
        }

        #endregion

        /// <summary>
        /// Загрузка портфеля
        /// </summary>
        private void GetPortfolio()
        {
            try
            {
                var tradeCode = Parameters["TradeCode"].ToString();

                SetResult(Server.Portfolios.SingleOrDefault(s => s.TradeCode == tradeCode));
            }
            catch (Exception e)
            {
                if (!(e is SqlException))
                    throw new Exception(String.Format("Ошибка  в параметрах"));
                throw;
            }
        }
    }
}
