namespace Risk
{
    /// <summary>
    /// Таблица AutoMarginCallInfos
    /// </summary>
    [Table("AutoMarginCallInfos", KeyFields = "TradeCode,InstrumentCode")]
    public class AutoMarginCallInfos : Table<AutoMarginCallInfo>
    {
    }
}
