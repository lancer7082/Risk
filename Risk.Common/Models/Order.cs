using System;

namespace Risk
{
    // Направление заявки
    public enum OrderType { Buy = 0, Sell }

    /// <summary>
    /// Поручение
    /// </summary>
    [Serializable]
    public class Order : ICloneable
    {
        /// <summary>
        /// ИД поручения (внутренний)
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Номер поручения
        /// </summary>
        public long OrderNo { get; set; }

        /// <summary>
        /// Дата поручения
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Торговый код
        /// </summary>
        public string TradeCode { get; set; }

        /// <summary>
        /// Код инструмента
        /// </summary>
        public string SecСode { get; set; }

        /// <summary>
        /// Кол-во в заявке
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Цена
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Направление заявки (тип)
        /// </summary>
        public OrderType OrderType { get; set; }

        /// <summary>
        /// Признак поручения, созданного в связи с Margin Call
        /// </summary>
        public bool MarginCall { get; set; }

        public object Clone()
        {
            return new Order
            {
                OrderId = this.OrderId,
                OrderNo = this.OrderNo,
                Date = this.Date, 
                OrderType = this.OrderType,
                Price = this.Price,
                Quantity = this.Quantity,
                SecСode = this.SecСode,
                TradeCode = this.TradeCode,
                MarginCall = this.MarginCall,
            };
        }
    }
}
