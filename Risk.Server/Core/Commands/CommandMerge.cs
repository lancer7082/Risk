using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Слияние строк таблицы
    /// </summary>
    [Command("Merge")]
    public class CommandMerge : CommandUpdate
    {
        /// <summary>
        /// Ключ в рамках которого, будет происходить слияние строк
        /// </summary>
        public string KeyFields 
        {
            get { return (string)Parameters["KeyFields"]; }
            set { Parameters["KeyFields"] = value; }
        }

        protected internal override void InternalExecute()
        {
            if (Data == null)
                Data = Table.CreateDataFromParams(Parameters, false);

            Table.Merge(Data, Fields, KeyFields);
        }
    }
}