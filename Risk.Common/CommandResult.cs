using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Результат выполнения команды
    /// </summary>
    [Serializable]
    [DataContract]
    [KnownType(typeof(Rate[]))]
    [KnownType(typeof(PortfolioRuleInfo[]))]
    [KnownType(typeof(AlertInfo[]))]
    [KnownType(typeof(Trade[]))]
    [KnownType(typeof(Client[]))]
    [KnownType(typeof(Portfolio[]))]
    [KnownType(typeof(Position[]))]
    [KnownType(typeof(Trade[]))]
    [KnownType(typeof(Order[]))]
    [KnownType(typeof(ConnectionInfo[]))]
    [KnownType(typeof(RiskSettings))]
    [KnownType(typeof(HierarchyObject[]))]
    [KnownType(typeof(Instrument[]))]
    public class CommandResult
    {
        [DataMember]
        public object Data { get; set; }

        [DataMember]
        public string[] TraceInfo { get; set; }

        // TODO: !!! Metadata
        // Fields ( Name, ReadOnly, Caption, StringSize )
    }
}