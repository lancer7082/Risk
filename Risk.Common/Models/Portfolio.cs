using System;

namespace Risk
{
    /// <summary>
    /// Портфель клиента
    /// </summary>
    [Serializable]
    public class Portfolio : ICloneable // TODO: ??? : NotifyData, IExtensibleDataObject, INotifyPropertyChanged
    {
        // !!! FOR DEBUG !!!
        public TimeSpan UpdateTime { get; set; }

        /// <summary>
        /// Id счета
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// ФИО \ наименование клиента
        /// </summary>
        public string Client { get; set; }

        /// <summary>
        /// Торговый код
        /// </summary>
        public string TradeCode { get; set; }

        /// <summary>
        /// Кодовое слово
        /// </summary>
        public string CodeWord { get; set; }

        private string _currency;

        /// <summary>
        /// Валюта счета
        /// </summary>
        public string Currency { 
            get {
                return _currency; 
            }

            set {
                _currency = value;
            }
        }         

        /// <summary>
        /// Ввод ДС: от открытия счета
        /// </summary>
        public decimal MoneyInInit { get; set; }

        /// <summary>
        /// Ввод ДС: на текущую дату
        /// </summary>
        public decimal MoneyInDay { get; set; }

        /// <summary>
        /// Вывод ДС: от открытия счета
        /// </summary>
        public decimal MoneyOutInit { get; set; }

        /// <summary>
        /// Вывод ДС: на текущую дату
        /// </summary>
        public decimal MoneyOutDay { get; set; }

        /// <summary>
        /// Текущая позиция (капитал) (tagPortfolioMCT -> m_dCapital)
        /// </summary>
        public decimal Capital { get; set; }

        /// <summary>
        /// Обеспеченность факт (tagPortfolioMCT -> m_dCoverageFact)
        /// </summary>
        public decimal CoverageFact { get; set; }

        /// <summary>
        /// Приб/убыт по входящ + Приб/убыт по сделкам 
        /// (tagPortfolioMCT -> m_dPLInit + m_dPLDay)
        /// </summary>
        public decimal PL { get; set; }

        /// <summary>
        /// Оборот - рассчитанный торговый оборот по клиенту \ инструменту
        /// </summary>
        public decimal Turnover { get; set; }

        /// <summary>
        /// Оборот в USD
        /// </summary>
        public decimal TurnoverCurrencyCalc {get; set; }

        /// <summary>
        /// Торги доступны
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Если true - Счет центрального контрагента
        /// </summary>
        public bool Contragent { get; set; }

        /// <summary>
        /// Баланс (Обеспеченность) в валюте расчетов
        /// </summary>
        public decimal CapitalCurrencyCalc { get; set; }

        /// <summary>
        /// входящее сальдо по ДС (tagPortfolioMCT -> m_dOpenBalance)
        /// </summary>
        public decimal OpenBalance { get; set; }

        /// <summary>
        /// Фин.рез: на начало сессии
        /// </summary>
        public decimal FinRes { get; set; }

        /// <summary>
        /// Прибыль в валюте расчетов
        /// </summary>
        public decimal PLCurrencyCalc { get; set; }

        /// <summary>
        /// Использование капитала, факт .0 %% 
        /// (tagPortfolioMCT -> )
        /// </summary>
        public decimal UtilizationFact { get; set; }

        /// <summary>
        /// Признак необходимости закрытия позиций по портфелю
        /// </summary>
        public bool MarginCall { get; set; }

        public override string ToString()
        {
            return String.Format("{0} ({1})", Client, TradeCode);
        }

        public object Clone()
        {
            return new Portfolio
            {
                 UpdateTime = this.UpdateTime,
                 AccountId  = this.AccountId,
                 Client     = this.Client,
                 TradeCode  = this.TradeCode,
                 CodeWord   = this.CodeWord,
                 Currency   = this.Currency,
                 MoneyInInit = this.MoneyInInit,
                 MoneyInDay = this.MoneyInDay,
                 MoneyOutInit = this.MoneyOutInit,
                 MoneyOutDay = this.MoneyOutDay,
                 Capital    = this.Capital,
                 CoverageFact = this.CoverageFact,
                 PL         = this.PL,
                 Turnover   = this.Turnover,
                 TurnoverCurrencyCalc = this.TurnoverCurrencyCalc,
                 Active     = this.Active,
                 Contragent = this.Contragent,
                 OpenBalance = this.OpenBalance,
                 FinRes     = this.FinRes,
                 PLCurrencyCalc = this.PLCurrencyCalc,
                 UtilizationFact = this.UtilizationFact,
                 MarginCall = this.MarginCall,
            };
    }
}
}
