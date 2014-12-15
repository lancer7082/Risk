namespace Risk.Commands
{
    /// <summary>
    /// Команда загрузки данных в начале новой сессии
    /// </summary>
    [Command("LoadNewSessionData")]
    public class CommandLoadNewSessionData : CommandServer
    {
        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            // очищаем серверные таблицы
            ServerBase.Current.Execute(Command.Delete("ExchangeRates", Server.ExchangeRates));
            ServerBase.Current.Execute(Command.Delete("Positions", Server.Positions));
            ServerBase.Current.Execute(Command.Delete("FinancialResults", Server.FinancialResults));
            ServerBase.Current.Execute(Command.Delete("Portfolios", Server.Portfolios));
            ServerBase.Current.Execute(Command.Delete("PositionsInstruments", Server.PositionsInstruments));
            ServerBase.Current.Execute(Command.Delete("Trades", Server.Trades));

            // запуск джобов загрузки данных - стартуют однократно
            ServerBase.Current.JobManager.RestartJob("LoadInstrumentsJob");
            ServerBase.Current.JobManager.RestartJob("GetInstrumentGroups");
            ServerBase.Current.JobManager.RestartJob("UpdateFinancialResultsJob");
            ServerBase.Current.JobManager.RestartJob("LoadInstrumentsGOInfo");
            ServerBase.Current.JobManager.RestartJob("UpdateExchangeRatesJob");
            ServerBase.Current.JobManager.RestartJob("UpdatePortfoliosJob");
            ServerBase.Current.JobManager.RestartJob("UpdatePortfoliosMoneyInOutDayJob");
        }
        #endregion
    }
}
