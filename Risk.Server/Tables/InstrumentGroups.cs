using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Таблица групп инструментов
    /// </summary>
    [Table("InstrumentGroups", KeyFields = "Id")]
    public class InstrumentGroups : Table<InstrumentGroup>
    {
        public override void TriggerAfter(TriggerCollection<InstrumentGroup> items)
        {
            base.TriggerAfter(items);
        }
    }
}
