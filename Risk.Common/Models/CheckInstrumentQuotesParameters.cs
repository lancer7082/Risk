using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Параметры проверки котировок для инструментов
    /// </summary>
    [Serializable]
    public class CheckInstrumentQuotesParameters
    {
        /// <summary>
        /// Торговый код
        /// </summary>
        public string TradeCode { get; set; }

        /// <summary>
        /// Код инструмента
        /// </summary>
        public string InstrumentCode { get; set; }
    }
}
