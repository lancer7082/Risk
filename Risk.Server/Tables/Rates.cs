using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Таблица курсов
    /// </summary>
    [Table("Rates", KeyFields = "CurrencyFrom,CurrencyTo")]
    public class Rates : Table<Rate>
    {        
    }
}
