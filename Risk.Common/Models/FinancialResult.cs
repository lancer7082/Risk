using System;

namespace Risk
{
    /// <summary>
    /// Финансовый результат
    /// </summary>
    [Serializable]
    public class FinancialResult : ICloneable
    {

        /// <summary>
        /// Торговый код
        /// </summary>
        public string TradeCode { get; set; }

        /// <summary>
        /// Код инстурмента
        /// </summary>
        public string SecCode { get; set; }

        /// <summary>
        /// Финансовый результат
        /// </summary>
        public decimal FinRes { get; set; }

        /// <summary>
        /// Финансовый результат в валюте отображения
        /// </summary>
        public decimal FinResCurrencyDisplay { get; set; }


        #region Implementation of ICloneable

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            return new FinancialResult
            {
                FinRes = FinRes,
                FinResCurrencyDisplay = FinResCurrencyDisplay,
                SecCode = SecCode,
                TradeCode = TradeCode
            };
        }
        #endregion
    }
}