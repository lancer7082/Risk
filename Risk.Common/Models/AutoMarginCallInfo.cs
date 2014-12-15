using System;

namespace Risk
{
    /// <summary>
    /// AutoMarginCallInfo
    /// </summary>
    [Serializable]
    public class AutoMarginCallInfo : ICloneable
    {
        public AutoMarginCallInfo()
        {
            Id = Guid.NewGuid();
            UpdateTime = DateTime.Now;
        }

        public Guid Id { get; set; }

        public string TradeCode { get; set; }
        public string InstrumentCode { get; set; }
        public decimal InstrumentGORate { get; set; }

        public DateTime UpdateTime { get; set; }

        public decimal CurrentCapital { get; set; }
        public decimal CapitalUsageOriginal { get; set; }
        public decimal PlannedCapitalUsage { get; set; }
        public decimal CapitalUsageNew { get; set; }

        public decimal QuantityPlanned { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal QuantityForClose { get; set; }

        public decimal PositionBalance { get; set; }
        public decimal PositionBalanceNew { get; set; }
        public decimal PositionGO { get; set; }
        public decimal PositionGoNew { get; set; }

        public decimal PositionPrice { get; set; }
        public decimal PositionQuote { get; set; }
        public decimal PositionsCount { get; set; }
        public decimal OtherPositionsGOSum { get; set; }

        public decimal MarginMin { get; set; }
        public string ClientName { get; set; }

        public object Clone()
        {
            return new AutoMarginCallInfo
            {
                Id = Id,
                TradeCode = TradeCode,
                InstrumentCode = InstrumentCode,
                InstrumentGORate = InstrumentGORate,
                UpdateTime = UpdateTime,

                CurrentCapital = CurrentCapital,
                CapitalUsageOriginal = CapitalUsageOriginal,
                PlannedCapitalUsage = PlannedCapitalUsage,
                CapitalUsageNew = CapitalUsageNew,

                PositionBalance = PositionBalance,
                PositionBalanceNew = PositionBalanceNew,
                PositionGoNew = PositionGoNew,
                PositionGO = PositionGO,
                PositionPrice = PositionPrice,
                PositionQuote = PositionQuote,
                PositionsCount = PositionsCount,
                OtherPositionsGOSum = OtherPositionsGOSum,
                CurrentQuantity = CurrentQuantity,
                QuantityForClose = QuantityForClose,
                QuantityPlanned = QuantityPlanned,
                MarginMin = MarginMin,
                ClientName = ClientName
            };
        }
    }
}
