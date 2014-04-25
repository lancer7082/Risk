using System;

namespace Risk
{
    /// <summary>
    /// Курс
    /// </summary>
    [Serializable]
    public class Rate : ICloneable
    {
        public string CurrencyFrom { get; set; }
        public string CurrencyTo { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }

        public object Clone()
        {
            return new Rate
            {
                CurrencyFrom = this.CurrencyFrom,
                CurrencyTo = this.CurrencyTo,
                Value = this.Value,
                Date = this.Date,
            };
        }
    }
}
