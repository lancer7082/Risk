using System;

namespace Risk
{
    /// <summary>
    /// Информация по ставкам ГО инструмента
    /// </summary>
    [Serializable]
    public class InstrumentGOInfo : ICloneable
    {
        /// <summary>
        /// Код инструмента
        /// </summary>
        public string SecCode { get; set; }

        /// <summary>
        /// Дневная ГО
        /// </summary>
        public decimal GORateDay { get; set; }

        /// <summary>
        /// Ночная ГО
        /// </summary>
        public decimal GORateNight { get; set; }

        /// <summary>
        /// Начало действия дневной ГО
        /// </summary>
        public TimeSpan? TimeDay { get; set; }

        /// <summary>
        /// Начало действия ночной ГО
        /// </summary>
        public TimeSpan? TimeNight { get; set; }

        /// <summary>
        /// Начало торгов
        /// </summary>
        public string TradeTimeBegin { get; set; }

        /// <summary>
        /// Завершение торгов
        /// </summary>
        public string TradeTimeEnd { get; set; }

        /// <summary>
        /// Начало торгов
        /// </summary>
        public DateTimeOffset? TradeDateBegin { get; set; }

        /// <summary>
        /// Завершение торгов
        /// </summary>
        public DateTimeOffset? TradeDateEnd { get; set; }

        /// <summary>
        /// Информация о паузах торгов
        /// </summary>
        public string TradePeriodParams { get; set; }

        /// <summary>
        /// Группа рынка
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Временная зона для времени торгов
        /// </summary>
        public string TradeTimeZone { get; set; }

        /// <summary>
        /// Полное расписание торгов
        /// </summary>
        public string sc_schedule { get; set; }

        /// <summary>
        /// Смещение времени (часовой пояс)
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Клонирование
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new InstrumentGOInfo
            {
                SecCode = SecCode,
                GORateDay = GORateDay,
                GORateNight = GORateNight,
                TimeDay = TimeDay,
                TimeNight = TimeNight,
                GroupName = GroupName,
                TradeTimeBegin = TradeTimeBegin,
                TradeTimeEnd = TradeTimeEnd,
                Offset = Offset,
                TradeDateBegin = TradeDateBegin,
                TradeDateEnd = TradeDateEnd,
                TradePeriodParams = TradePeriodParams,
                TradeTimeZone = TradeTimeZone,
                sc_schedule = sc_schedule
            };
        }
    }
}
