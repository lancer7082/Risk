using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [Serializable]
    public class Trade
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
        public string Seccode { get; set; }

        public string TradeType { get; set; }
        public DateTime TradeTime { get; set; }
        public bool Sell { get; set; }

        public long TradeNo { get; set; }
        public long OrderNo { get; set; }
        public double Price { get; set; }
        public double Yield { get; set; }
        public long Quantity { get; set; }
        public double Value { get; set; }
        public double Comission { get; set; }
    }
}
