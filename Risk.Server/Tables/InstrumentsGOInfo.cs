namespace Risk
{
    /// <summary>
    /// Информация о ставках ГО инструментов
    /// </summary>
    [Table("InstrumentsGOInfo", KeyFields = "SecCode")]
    public class InstrumentsGOInfo : Table<InstrumentGOInfo>
    {
    }
}
