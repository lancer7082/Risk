using Risk;

namespace Risk.JobManager.Jobs
{
    /// <summary>
    /// Джоб обновления курсов
    /// </summary>
    public class UpdateExchangeRatesJob : CommandServerJob
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="connectionString">Строка подключения к БД</param>
        /// <param name="firmId"></param>
        /// <param name="commandTimeout">Таймаут выполнения команды</param>
        /// <param name="dataObject"></param>
        public UpdateExchangeRatesJob(string connectionString, byte firmId, int commandTimeout, IDataObject dataObject)
            : base(connectionString, firmId, commandTimeout, dataObject)
        {
        }

        #region Overrides of CommandServerJob

        /// <summary>
        /// Непосредственное выполнение джоба
        /// </summary>
        protected override void ExecuteConcreteJob()
        {
            new CommandMerge
            {
                Object = DataObject,
                Data = DatabaseWorker.GetRates(ServerBase.Current.ServerTime, Server.Settings.CurrencyCalc),
                Fields = "Value,Date",
            }.Execute();
        }

        #endregion
    }
}