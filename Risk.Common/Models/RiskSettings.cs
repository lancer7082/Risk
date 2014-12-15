using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Способы оповещений
    /// </summary>
    [Flags]
    public enum NotifyType : int
    {
        None = 0x0,
        Terminal = 0x1,
        Email = 0x2,
        SMS = 0x4
    }

    /// <summary>
    /// Опции
    /// </summary>
    [Serializable]
    public class RiskSettings
    {
        #region  Оповещения администратора (пользователя)

        /// <summary>
        /// Оповещать пользователя о деятельности клиентов
        /// </summary>
        public bool NotifyAdmin { get; set; }

        /// <summary>
        /// Оповещать пользователя о превышении прибыли клиента
        /// </summary>
        public bool NotifyAdminProfit { get; set; }

        /// <summary>
        /// Максимальная сумма прибыли (USD)
        /// </summary>
        public decimal MaxSumProfit { get; set; }

        /// <summary>
        /// Максимальный процент прибыли от входящего капитала
        /// </summary>
        public ushort MaxPercentProfit { get; set; }

        /// <summary>
        /// Оповещать пользователя о превышении оборота клиента
        /// </summary>
        public bool NotifyAdminTurnover { get; set; }

        /// <summary>
        /// Максимальная сумма оборота (USD)
        /// </summary>
        public decimal MaxSumTurnover { get; set; }

        /// <summary>
        /// Максимальный процент оборота от входящего капитала
        /// </summary>
        public ushort MaxPercentTurnover { get; set; }

        #endregion

        #region  Margin call

        /// <summary>
        /// Закрывать позиции в случае Margin Call
        /// </summary>
        public bool MarginForceClose { get; set; }

        /// <summary>
        /// Автоматически закрывать позиции
        /// </summary>
        public bool AutoMarginCallEnabled { get; set; }

        /// <summary>
        /// % использования капитала для наступления Margin Call
        /// </summary>
        public ushort MaxPercentUtilMarginCall { get; set; }

        /// <summary>
        /// Оповещать клиентов о превышении % использования капитала
        /// </summary>
        public bool NotifyClientMaxPercentUtilExceed { get; set; }

        /// <summary>
        /// % использования капитала для предупреждения клиентов
        /// </summary>
        public ushort MaxPercentUtilWarning { get; set; }

        /// <summary>
        /// Способ оповещения клиента о превышении % использования капитала
        /// </summary>
        public NotifyType NotifyTypeMaxPercentUtilWarning { get; set; }

        /// <summary>
        /// Способ оповещения клиента о Margin call
        /// </summary>
        public NotifyType NotifyTypeMaxPercentUtilMarginCall { get; set; }

        /// <summary>
        /// Плановое использование капитала
        /// </summary>
        public int PlannedCapitalUtilization { get; set; }

        #endregion

        /// <summary>
        /// Валюта для расчетов
        /// <remarks>Для приведения сумм к единой валюте при отображении на клиенте и в расчетах</remarks>
        /// </summary>
        public string CurrencyCalc { get; set; }

        /// <summary>
        /// Валюта отображения на клиенте
        /// </summary>
        public string CurrencyDisplay { get; set; }

        /// <summary>
        /// Инструменты по которым нужно проверять цены
        /// </summary>
        public string CheckingQuotesInstruments { get; set; }

        /// <summary>
        /// Период проверки цен по инструментам
        /// </summary>
        public int CheckingQuotesPeriod { get; set; }

        /// <summary>
        /// Текущее время сервера
        /// </summary>
        public DateTime ServerTime
        {
            get { return DateTime.Now; }
        }

        /// <summary>
        /// Смещение UTC текущего часового пояса
        /// </summary>
        public int UtcOffset
        {
            get { return (int)TimeZoneInfo.Local.GetUtcOffset(ServerTime).TotalMinutes; }
        }

        /// <summary>
        /// Интервал (в секундах) между сообщениями о Margin Call для клиентского приложения
        /// </summary>
        public int MarginCallAlertInterval { get; set; }

        public RiskSettings()
        {
            // Настройки по умолчанию
            // TODO: ??? From config
            NotifyAdmin = true;
            NotifyAdminProfit = true;
            MaxSumProfit = 10000;
            MaxPercentProfit = 50;
            NotifyAdminTurnover = true;
            MaxSumTurnover = 1000000;
            MaxPercentTurnover = 1000;
            // Margin Call
            MarginForceClose = false;
            MaxPercentUtilMarginCall = 200;
            NotifyClientMaxPercentUtilExceed = true;
            MaxPercentUtilWarning = 150;
            NotifyTypeMaxPercentUtilWarning = NotifyType.Terminal | NotifyType.Email;
            NotifyTypeMaxPercentUtilMarginCall = NotifyType.Terminal;
            //
            CurrencyCalc = "USD";
            CurrencyDisplay = "USD";
            PlannedCapitalUtilization = 100;
            CheckingQuotesInstruments = "FB.O;GE;SBER.MM;SNGS.MM;TATN.MM";
            CheckingQuotesPeriod = 10;
            AutoMarginCallEnabled = false;
            MarginCallAlertInterval = 10;
        }
    }
}
