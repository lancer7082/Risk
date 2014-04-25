using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public static class EnumerableHelpers
    {
        public static decimal CumulativeSum<T>(this IEnumerable<TriggerPair<T>> source, Func<T, decimal> selectorValue)
            where T : new()
        {
            return source.Select<TriggerPair<T>, decimal>(x =>
                x.Deleted == null
                ?
                   x.Inserted == null ? 0 : selectorValue(x.Updated)
                :
                   x.Inserted == null ? -selectorValue(x.Deleted) : selectorValue(x.Updated) - selectorValue(x.Deleted)).Sum();
        }

        public static int CumulativeSum<T>(this IEnumerable<TriggerPair<T>> source, Func<T, int> selectorValue)
            where T : new()
        {
            return source.Select<TriggerPair<T>, int>(x => x.Deleted == null ? x.Inserted == null ? 0 : selectorValue(x.Updated) : x.Inserted == null ? -selectorValue(x.Deleted) : selectorValue(x.Updated) - selectorValue(x.Deleted)).Sum();
        }
    }
}
