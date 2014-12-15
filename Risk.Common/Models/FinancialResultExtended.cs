using System;

namespace Risk
{
    /// <summary>
    /// Финансовый результат
    /// </summary>
    [Serializable]
    public class FinancialResultExtended : FinancialResult
    {
        public FinancialResultExtended()
        {
        }

        public FinancialResultExtended(FinancialResult financialResult)
        {
            FinRes = financialResult.FinRes;
            FinResCurrencyDisplay = financialResult.FinResCurrencyDisplay;
            SecCode = financialResult.SecCode;
            TradeCode = financialResult.TradeCode;
        }

        /// <summary>
        /// Название инструмента
        /// </summary>
        public string InstrumentName { get; set; }

        /// <summary>
        /// Код класса инструмента
        /// </summary>
        public string InstrumentClassCode { get; set; }

        /// <summary>
        /// Имя класса инструмента
        /// </summary>
        public string InstrumentClassName { get; set; }

        /// <summary>
        /// Имя клиента
        /// </summary>
        public string ClientName { get; set; }

        #region Implementation of ICloneable

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public new object Clone()
        {
          return new FinancialResultExtended
            {
                FinRes = FinRes,
                FinResCurrencyDisplay = FinResCurrencyDisplay,
                SecCode = SecCode,
                TradeCode = TradeCode,
                ClientName = ClientName,
                InstrumentClassName = InstrumentClassName,
                InstrumentClassCode = InstrumentClassCode,
                InstrumentName = InstrumentName,
            };
        }

        #endregion
    }
}