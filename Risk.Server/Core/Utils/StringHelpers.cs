using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Набор расширений для строк
    /// </summary>
    public static class StringHelpers
    {
        public static string NullIfEmpty(this string s)
        {
            if (String.IsNullOrEmpty(s))
                return null;
            else
                return s;
        }
    }
}
