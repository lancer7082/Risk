namespace Risk
{
    /// <summary>
    /// Таблица AccountsLimits
    /// </summary>
    [Table("AccountsLimits", KeyFields = "Market")]
    public class AccountsLimits : Table<AccountLimit>
    {
    }
}
