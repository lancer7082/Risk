using System;

namespace Risk
{
    /// <summary>
    /// Сделка
    /// </summary>
    [Serializable]
    public class Trade : ICloneable
    {
        /// <summary>
        /// Id счета
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Код счета
        /// </summary>
        public string TradeCode { get; set; }

        /// <summary>
        /// Код инструмента
        /// </summary>
        public string SecCode { get; set; }

        /// <summary>
        /// Тип сделки (Buy | Sell)
        /// </summary>
         public string TradeType { get; set; }
        public DateTime TradeTime { get; set; }
        public bool Sell { get; set; }

        public long TradeNo { get; set; }
        public long OrderNo { get; set; }
        public double Price { get; set; }
        public double Yield { get; set; }
        public long Quantity { get; set; }
        public decimal Value { get; set; }
        public double Commission { get; set; }

        /// <summary>
        /// Рассчитанная сумма
        /// </summary>
        public decimal ValueCalc { get; set; }

        public object Clone()
        {
            return new Trade
            {
                AccountId = this.AccountId,
                TradeCode = this.TradeCode,
                SecCode = this.SecCode,
                TradeType = this.TradeType,
                TradeTime = this.TradeTime,
                Sell = this.Sell,

                TradeNo = this.TradeNo,
                OrderNo = this.OrderNo,
                Price = this.Price,
                Yield = this.Yield,
                Quantity = this.Quantity,
                Value = this.Value,
                Commission = this.Commission,

                ValueCalc = this.ValueCalc,
            };
        }
    }
}
