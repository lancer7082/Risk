using System;
using System.Runtime.Serialization;

namespace Risk
{
    public enum RuleType
    {
        /// <summary>
        /// Нет
        /// </summary>
        None,

        /// <summary>
        /// Превышение прибыли
        /// </summary>
        MaxProfitExceed,

        /// <summary>
        /// Превышение процента прибыли от входящего капитала
        /// </summary>
        MaxPercentProfitExceed,

        /// <summary>
        /// Превышение оборота по сделкам
        /// </summary>
        MaxTurnoverExceed,

        /// <summary>
        /// Превышение процента оборота по сделкам от входящего капитала
        /// </summary>
        MaxPercentTurnoverExceed,

        /// <summary>
        /// Превышение % использования капитала, при которой закрывается позиция
        /// </summary>
        MaxPercentUtilMarginCallExceed,

        /// <summary>
        /// Превышение % использования капитала, при которой отправляется уведомление клиенту
        /// </summary>
        MaxPercentUtilWarningExceed,

        /// <summary>
        /// Некорректная ставка ГО
        /// </summary>
        IncorrectGORate,

        /// <summary>
        /// Отсутсвуют рыночные данные по инструменту
        /// </summary>
        NoInstrumentsQuotes,

        /// <summary>
        /// Мониторинг внутредневных вводов/выводов
        /// </summary>
        IODailyMonitoring,

        ScalperTrade
    }

    [Serializable]
    [DataContract]
    public class PortfolioRule : ICloneable
    {
        private Portfolio _portfolio;

        private Alert _lastAlert;

        /// <summary>
        ///  ИД правила
        /// </summary>
        [DataMember]
        public RuleType RuleType { get; set; }

        /// <summary>
        /// RuleTypeText
        /// </summary>
        [DataMember]
        public string RuleTypeText
        {
            get { return RuleType.ToString(); }
            private set { }
        }

        /// <summary>
        /// Объект, к которому применяются правила 
        /// <remarks> Сейчас Portfolios </remarks>
        /// </summary>
        [IgnoreDataMember]
        public Portfolio Portfolio
        {
            get { return _portfolio; }
            set
            {
                _portfolio = value;
                TradeCode = value.TradeCode;
            }
        }

        /// <summary>
        /// TradeCode
        /// </summary>
        [DataMember]
        public string TradeCode { get; private set; }

        /// <summary>
        /// Время первого срабатывания правила 
        /// </summary>
        [DataMember]
        public DateTime RuleTime { get; set; }

        /// <summary>
        /// Последнее оповещение по правилу
        /// </summary>
        [IgnoreDataMember]
        public Alert LastAlert
        {
            get { return _lastAlert; }
            set
            {
                _lastAlert = value;
                LastAlertTime = value.DateTime;
                LastAlertText = value.Text;
            }
        }

        /// <summary>
        /// LastAlertText
        /// </summary>
        [DataMember]
        public string LastAlertText { get; private set; }

        /// <summary>
        /// LastAlertTime
        /// </summary>
        [DataMember]
        public DateTime LastAlertTime { get; private set; }

        /// <summary>
        /// Совершенные способы уведомления о событии
        /// </summary>
        [IgnoreDataMember]
        public NotifyType NotifyTypesAccomplished { get; set; }

        /// <summary>
        /// Всегда отправлять алерт, без анализа его времени устаревания
        /// </summary>
        [IgnoreDataMember]
        public bool AlwaysSend { get; set; }

        /// <summary>
        /// ICloneable
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new PortfolioRule
            {
                RuleType = RuleType,
                Portfolio = Portfolio,
                RuleTime = RuleTime,
                LastAlert = LastAlert,
                NotifyTypesAccomplished = NotifyTypesAccomplished,
                LastAlertText = LastAlertText,
                LastAlertTime = LastAlertTime,
                TradeCode = TradeCode,
                AlwaysSend = AlwaysSend
            };
        }
    }
}
