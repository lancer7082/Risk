using System;
using System.Data.SqlClient;
using System.Linq;

namespace Risk.Commands
{
    /// <summary>
    /// Команда изменяет торговые параметры инструментов
    /// </summary>
    [Command("ChangeInstrumentsParameters")]
    public class CommandChangeInstrumentsParameters : CommandServer
    {
        private const string CommandInstrumentsDelimiter = ",";

        private const string ServerCheckingInstrumentsQuotesDelimiter = ";";

        #region Overrides of CommandServer

        protected internal override void InternalExecute()
        {
            ChangeInstrumentsParameters();
        }

        #endregion

        /// <summary>
        /// Изменяет параметры инструментов
        /// </summary>
        private void ChangeInstrumentsParameters()
        {
            if (Connection != null)
                Connection.CheckDealerUser();
            try
            {
                // эти параметры должны быть заполнены всегда
                var enabled = Parameters["Enabled"] != null ? Convert.ToBoolean(Parameters["Enabled"].ToString()) : (bool?)null;
                var marketPermitted = Parameters["MarketPermitted"] != null ? Convert.ToBoolean(Parameters["MarketPermitted"].ToString()) : (bool?)null;
                var longPermitted = Parameters["LongPermitted"] != null ? Convert.ToBoolean(Parameters["LongPermitted"].ToString()) : (bool?)null;
                var shortPermitted = Parameters["ShortPermitted"] != null ? Convert.ToBoolean(Parameters["ShortPermitted"].ToString()) : (bool?)null;
                var resident = Parameters["Resident"] != null ? Convert.ToBoolean(Parameters["Resident"].ToString()) : (bool?)null;
                var notResident = Parameters["NotResident"] != null ? Convert.ToBoolean(Parameters["NotResident"].ToString()) : (bool?)null;
                var bsStopDeny = Parameters["BsStopDeny"] != null ? Convert.ToBoolean(Parameters["BsStopDeny"].ToString()) : (bool?)null;
                var bsStopDenyZone = Parameters["BsStopDenyZone"] != null ? Convert.ToInt32(Parameters["BsStopDenyZone"].ToString()) : (int?)null;
                var isQuotesChecking = Parameters["IsQuotesChecking"] != null ? Convert.ToBoolean(Parameters["IsQuotesChecking"].ToString()) : (bool?)null;

                var login = string.Empty;
                if (Connection != null)
                    login = Connection.UserName;

                var residentsBitMask = resident.HasValue ? Convert.ToInt32(resident) | (Convert.ToInt32(notResident) << 1) : (int?)null;

                // если коды инструментов заполнены, то берем их
                // если нет, то берем рынок
                var instrumentsCodes = Parameters["Codes"] != null ? Parameters["Codes"].ToString() : string.Empty;
                if (!string.IsNullOrEmpty(instrumentsCodes))
                {
                    instrumentsCodes = instrumentsCodes.Replace(Environment.NewLine, string.Empty).TrimEnd(CommandInstrumentsDelimiter.ToCharArray());
                    UpdateServerSettings(instrumentsCodes, isQuotesChecking);
                    ServerBase.Current.DataBase.ChangeInstrumentsParameters(instrumentsCodes, enabled,
                        marketPermitted, longPermitted, shortPermitted, residentsBitMask, bsStopDeny, bsStopDenyZone, login);
                }
                else
                {
                    // при пустых инструментах этот параметр должен бытьзаполнен обязательно
                    var market = Parameters["Market"].ToString();
                    var instruments = Server.Instruments.Where(s => s.ClassName == market).ToList();
                    if (instruments.Any())
                    {
                        instruments.ForEach(s => instrumentsCodes += s.SecCode + CommandInstrumentsDelimiter);
                        instrumentsCodes = instrumentsCodes.TrimEnd(CommandInstrumentsDelimiter.ToCharArray());

                        ServerBase.Current.DataBase.ChangeInstrumentsParameters(instrumentsCodes, enabled,
                            marketPermitted, longPermitted, shortPermitted, residentsBitMask,
                            bsStopDeny, bsStopDenyZone, login);
                    }
                }

                // перегружаем измененные данные из БД в таблицу инструментов
                ServerBase.Current.JobManager.RestartJob("LoadInstrumentsJob");
            }
            catch (Exception e)
            {
                if (!(e is SqlException))
                    throw new Exception(String.Format("Ошибка  в параметрах"));
                throw;
            }
        }

        /// <summary>
        /// Обновить настройки сервера
        /// </summary>
        /// <param name="instrumentsCodes"></param>
        /// <param name="isQuotesChecking"></param>
        private void UpdateServerSettings(string instrumentsCodes, bool? isQuotesChecking)
        {
            if (!isQuotesChecking.HasValue)
                return;

            var serverSettings = Server.Settings.CheckingQuotesInstruments ?? string.Empty;

            if (string.IsNullOrEmpty(serverSettings) || string.IsNullOrEmpty(instrumentsCodes))
                serverSettings = instrumentsCodes ?? string.Empty;
            else
            {
                // получаем списки инструментов из их строкого представления
                var splittedInstrumentsCodes = instrumentsCodes.Split(CommandInstrumentsDelimiter.ToCharArray()).ToList();
                var splittedserverSettings = serverSettings.Split(ServerCheckingInstrumentsQuotesDelimiter.ToCharArray()).ToList();

                // удаляем из настроек сервера все инструменты по которым была выключена проверка котировок
                // или добавляем в настройки сервера все инструменты по которым была включена эта проверка
                if (isQuotesChecking.Value)
                {
                    splittedserverSettings.AddRange(splittedInstrumentsCodes.Where(s => !splittedserverSettings.Contains(s)));
                }
                else
                {
                    splittedserverSettings.RemoveAll(splittedInstrumentsCodes.Contains);
                }

                // пересобираем строку с настройками сервера из списка
                serverSettings = splittedserverSettings.Aggregate(string.Empty, (s, s1) => s + s1 + ServerCheckingInstrumentsQuotesDelimiter);
                serverSettings = serverSettings.TrimEnd(ServerCheckingInstrumentsQuotesDelimiter.ToCharArray());
            }
            Server.Current.ExecuteAsync(Command.Update("Settings",
                new RiskSettings { CheckingQuotesInstruments = serverSettings }, "CheckingQuotesInstruments"));
        }
    }
}
