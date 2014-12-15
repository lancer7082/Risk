using System;
using System.Data.SqlClient;

namespace Risk.Commands
{
    /// <summary>
    /// Команда Изменяет торговые параметры клиента
    /// </summary>
    [Command("ChangeTradersAccountParameters")]
    public class CommandChangeTradersAccountParameters : CommandServer
    {
        private const string Delimiter = ",";

        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            ChangeTradersAccountParameters();
        }

        #endregion

        /// <summary>
        /// Изменяет торговые параметры клиента
        /// </summary>
        private void ChangeTradersAccountParameters()
        {
            if (Connection != null)
                Connection.CheckDealerUser();
            try
            {
                var login = string.Empty;
                if (Connection != null)
                    login = Connection.UserName;

                // торговые коды клиентов
                var tradeCodes = Parameters["TradeCodes"].ToString().Replace(Environment.NewLine, string.Empty).TrimEnd(Delimiter.ToCharArray());

                var bsStopDeny = Parameters["BsStopDeny"] != null ? Convert.ToBoolean(Parameters["BsStopDeny"].ToString()) : (bool?)null;
                var goCoeff = Parameters["GoCoeff"] != null ? Convert.ToInt32(Parameters["GoCoeff"].ToString()) : (int?)null;
                var accessAuction = Parameters["AccessAuction"] != null ? Convert.ToBoolean(Parameters["AccessAuction"].ToString()) : (bool?)null;
                var retain = Parameters["Retain"] != null ? Parameters["Retain"].ToString() : null;

                ServerBase.Current.DataBase.ChangeTraderAccountParameters(tradeCodes, bsStopDeny, goCoeff, accessAuction, retain, login);

                // перегружаем измененные данные из БД в таблицу портфелей
                ServerBase.Current.JobManager.RestartJob("UpdatePortfoliosJob");
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
