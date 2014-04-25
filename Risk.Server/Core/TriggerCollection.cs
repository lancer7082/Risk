using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class TriggerCollection<T> : ReadOnlyCollection<TriggerPair<T>>
        where T : new()
    {
        public IEnumerable<T> Inserted
        {
            get { return this.Where(x => x.Inserted != null && x.Deleted == null).Select(x => x.Inserted); }
        }

        public IEnumerable<T> Updated
        {
            get { return this.Select(x => x.Updated); }
        }

        public IEnumerable<T> Deleted
        {
            get { return this.Where(x => x.Deleted != null && x.Inserted == null).Select(x => x.Deleted); }
        }

        internal TriggerCollection(IList<TriggerPair<T>> items)
            : base(items)
        {
        }
    }
}