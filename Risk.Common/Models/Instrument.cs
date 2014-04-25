using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Инструмент
    /// </summary>
    [Serializable]
    public class Instrument : ICloneable
    {
        /// <summary>
        /// Код инструмента
        /// </summary>
        public string SecCode { get; set; }

        /// <summary>
        /// Валюта инструмента
        /// </summary>
        public string SecurityCurrency { get; set; }

        /// <summary>
        /// Кол-во знаков после запятой в цене
        /// </summary>
        public int Decimals { get; set; }

        /// <summary>
        /// Стоимость шага цены
        /// </summary>
        public int Bpcost { get; set; }

        /// <summary>
        /// Размер лота
        /// </summary>
        public int Lotsize { get; set; }

        public object Clone()
        {
            return new Instrument
            {
                SecCode  = this.SecCode,
                SecurityCurrency = this.SecurityCurrency,
                Decimals = this.Decimals,
                Bpcost   = this.Bpcost,
                Lotsize  = this.Lotsize
            };
        }
    }
}
