using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Risk.Commands
{
    /// <summary>
    /// Загрузка финансового результата
    /// </summary>
    [Command("LoadFinancialResults")]
    public class CommandLoadFinancialResults : CommandServer
    {
        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            LoadFinancialResults();
        }

        #endregion

        /// <summary>
        /// Загрузка финансового результата
        /// </summary>
        private void LoadFinancialResults()
        {
            try
            {
                DateTime dateFrom, dateTo;

                // получаем даты
                Alerts.GetDatesParameters(Parameters, out dateFrom, out dateTo);

                // загружаем финрез
                var finRes = ServerBase.Current.DataBase.LoadFinancialResults(
                            ServerBase.Current.ServerConfigurationSection.Server.FirmId.Value,
                            Server.Settings.CurrencyDisplay,
                            dateFrom == DateTime.MinValue ? (DateTime?)null : dateFrom,
                            dateTo == DateTime.MaxValue ? (DateTime?)null : dateTo);

                var finResExtented = InsertAdditionalData(finRes.ToList());

                // возвращаем финрез
                SetResult(finResExtented.ToArray());
            }
            catch (Exception e)
            {
                if (!(e is SqlException))
                    throw new Exception(String.Format("Ошибка  в параметрах"));
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finRes"></param>
        /// <returns></returns>
        private List<FinancialResultExtended> InsertAdditionalData(List<FinancialResult> finRes)
        {
            var list = new List<FinancialResultExtended>();

            foreach (var financialResult in finRes)
            {
                var finResExtended = new FinancialResultExtended(financialResult);

                var instrument = Server.Instruments.SingleOrDefault(s => s.SecCode == financialResult.SecCode);
                if (instrument != null)
                {
                    finResExtended.InstrumentName = instrument.Name;
                    finResExtended.InstrumentClassCode = instrument.ClassCode;
                    finResExtended.InstrumentClassName = instrument.ClassName;
                }

                var portfolio = Server.Portfolios.SingleOrDefault(s => s.TradeCode == financialResult.TradeCode);
                if (portfolio != null)
                {
                    finResExtended.ClientName = portfolio.Client;
                }

                list.Add(finResExtended);
            }
            return list;
        }
    }
}
