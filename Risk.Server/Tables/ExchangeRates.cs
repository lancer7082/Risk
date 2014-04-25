using System;
using System.Collections.Generic;

namespace Risk
{
    /// <summary>
    /// Таблица курсов
    /// </summary>
    [Table("ExchangeRates", KeyFields = "CurrencyFrom,CurrencyTo")]
    public class ExchangeRates : Table<Rate>
    {
    }
}
