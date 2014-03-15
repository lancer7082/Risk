using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class MoneyInOut
    {
        /// <summary>
        /// Id счета
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Ввод ДС: на текущую дату
        /// </summary>
        public decimal MoneyInDay { get; set; }

        /// <summary>
        /// Вывод ДС: на текущую дату
        /// </summary>
        public decimal MoneyOutDay { get; set; }
    }
}
