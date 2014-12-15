using System;

namespace Risk
{
    [Serializable]
    public class AccountLimit
    {
        public string Market { get; set; }

        public string Currency { get; set; }

        public decimal GO { get; set; }

        public decimal Limit { get; set; }

        public decimal Free { get; set; }

        public DateTime DateTime { get; set; }
    }
}