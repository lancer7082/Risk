using System;

namespace Risk
{
    /// <summary>
    /// Данные на начало сессии
    /// - вводы / выводы ДС
    /// - фин. рез
    /// </summary>
    public class SessionInitialData 
    {
        /// <summary>
        /// Id счета
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Ввод ДС: на начало сессии
        /// </summary>
        public decimal MoneyInInit { get; set; }

        /// <summary>
        /// Вывод ДС: на начало сессии
        /// </summary>
        public decimal MoneyOutInit { get; set; }

        /// <summary>
        /// Итоговый результат = Финансовый результат + Комиссии: на начало сессии
        /// </summary>
        public decimal FinRes { get; set; }

        public static explicit operator Portfolio(SessionInitialData initData)
        {
            return new Portfolio
            {
                AccountId   = initData.AccountId,
                MoneyInInit = initData.MoneyInInit,
                MoneyOutInit = initData.MoneyOutInit,
                FinRes = initData.FinRes,
            };
        }
    }
}
