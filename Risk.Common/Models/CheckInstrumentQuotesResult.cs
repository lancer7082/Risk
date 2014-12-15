using System;

namespace Risk
{
    /// <summary>
    /// Результаты запроса котировок для инструмента
    /// </summary>
    [Serializable]
    public class CheckInstrumentQuotesResult
    {
        /// <summary>
        /// Цена
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// На покупку
        /// </summary>
        public double QuantityBid { get; set; }

        /// <summary>
        /// На продажу
        /// </summary>
        public double QuantitySell { get; set; }
    }
}