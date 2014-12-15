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
        #region Overrides of Table<Position>

        /// <summary>
        ///  Trigger on Add, Update, Delete
        /// </summary>
        public override void TriggerAfter(TriggerCollection<Position> items)
        {
            base.TriggerAfter(items);
            Positions.SetInstrumentData(items);
        }

        #endregion
    }
}
