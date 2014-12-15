using System;

namespace Risk
{
    /// <summary>
    /// Заявка во внешней системе (Этна)
    /// </summary>
    [Serializable]
    public class ETNAPosition
    {
        /// <summary>
        /// Идентификатор инструмента
        /// </summary>
        public int EtnaInstumentId { get; set; }

        /// <summary>
        /// Код инструмента (to_seccode)
        /// </summary>
        /// <remarks>
        /// Используется для связи внешних инструментов с внутренними
        /// </remarks>
        public string EtnaInstumentCode { get; set; }

        /// <summary>
        /// Количество лотов в  сделке (без учета размера лота)
        /// </summary>
        public decimal Quantity { get; set; }
    }
}
