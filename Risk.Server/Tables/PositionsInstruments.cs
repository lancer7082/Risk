using System.Collections.Generic;
using System.Linq;

namespace Risk
{
    /// <summary>
    /// Таблица позиций по инструментам
    /// </summary>
    [Table("PositionsInstruments", KeyFields = "SecCode")]
    public class PositionsInstruments : Table<Position>
    {
        public override void TriggerAfter(TriggerCollection<Position> items)
        {
        }
    }
}
