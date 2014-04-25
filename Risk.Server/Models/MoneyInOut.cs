using System;

namespace Risk
{
    /// <summary>
    /// Ввод-вывод дс по счету
    /// </summary>
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

        public static explicit operator Portfolio(MoneyInOut moneyInOut)
        {
            return new Portfolio
            {
                AccountId = moneyInOut.AccountId,
                MoneyInDay = moneyInOut.MoneyInDay,
                MoneyOutInit = moneyInOut.MoneyOutDay
            };
        }
    }
}
