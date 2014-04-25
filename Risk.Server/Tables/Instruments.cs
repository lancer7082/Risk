using System;
using System.Collections.Generic;

namespace Risk
{
    /// <summary>
    /// Инструменты
    /// </summary>
    [Table("Instruments", KeyFields = "SecCode")]
    public class Instruments : Table<Instrument>
    {
    }
}
