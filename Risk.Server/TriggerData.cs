using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class TriggerData<T>
    {
        public ActionType Action { get; private set; }
        public IReadOnlyCollection<TriggerPair<T>> Items { get; private set; }
        public string[] Fields { get; private set; }

        internal TriggerData(ActionType action, IReadOnlyCollection<TriggerPair<T>> updated, string[] fields)
        {
            this.Items = updated;
            this.Action = action;
        }
    }
}