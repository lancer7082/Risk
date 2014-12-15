using System;

namespace Risk
{
    /// <summary>
    /// Суммарная информация по позиции для сверки
    /// </summary>
    [Serializable]
    public class ReconciliationPosition
    {
        /// <summary>
        /// Код инструмента
        /// </summary>
        public string InstrumentCode { get; set; }

        /// <summary>
        /// Имя инструмента
        /// </summary>
        public string InstrumentName { get; set; }

        /// <summary>
        /// Суммарная позиция по инструменту в ММА в единицах с учетом размера лота инструмента
        /// </summary>
        public decimal QuantityMMA { get; set; }

        /// <summary>
        /// Суммарная позиция по инструменту во внешней системе с учетом размера лота инструмента
        /// </summary>
        public decimal QuantityExternal { get; set; }

        /// <summary>
        /// Разница
        /// </summary>
        public decimal Difference { get; set; }

        /// <summary>
        /// Код рынка
        /// </summary>
        public string ExternalMarketCode { get; set; }

        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message { get; set; }
    }
}
