using System;

namespace Risk
{
    /// <summary>
    /// Портфель клиента
    /// </summary>
    [Serializable]
    public class Portfolio : ICloneable
    {
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

        /// <summary>
        /// Валюта счета
        /// </summary>
        public string Currency { get; set; }

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
        public decimal TurnoverCurrencyCalc { get; set; }

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

        /// <summary>
        /// Минимальная маржа
        /// </summary>
        public decimal MarginMin { get; set; }

        /// <summary>
        /// Рефанд по торговому коду 
        /// </summary>
        /// <remarks>
        ///	Включен = true
        ///	Выключен = false
        /// </remarks>
        public bool IsRefund { get; set; }

        #region Признаки превышения показателей

        /// <summary>
        /// Признак превышения прибыли 
        /// </summary>
        public bool IsMaxProfitExceed { get; set; }

        /// <summary>
        /// Признак превышения процента прибыли от входящего капитала
        /// </summary>
        public bool IsMaxPercentProfitExceed { get; set; }

        /// <summary>
        /// Признак превышения оборота по сделкам
        /// </summary>
        public bool IsMaxTurnoverExceed { get; set; }

        /// <summary>
        /// Признак превышения процента оборота по сделкам от входящего капитала
        /// </summary>
        public bool IsMaxPercentTurnoverExceed { get; set; }

        /// <summary>
        /// Торговый оборот в валюте отображения
        /// <remarks>Правила расчета см. в ФТ 2</remarks>
        /// </summary>
        public decimal TurnoverCurrencyDisplay { get; set; }

        /// <summary>
        /// P\L в валюте отображения
        /// </summary>
        public decimal PLCurrencyDisplay { get; set; }

        /// <summary>
        /// Баланс (Обеспеченность) в валюте отображения
        /// </summary>
        public decimal CapitalCurrencyDisplay { get; set; }

        /// <summary>
        /// Client Email
        /// </summary>
        public string ClientEmail { get; set; }

        /// <summary>
        /// Доступ к торгам 
        /// </summary>
        public bool AccessAuction { get; set; }

        /// <summary>
        /// Коэффициент к стандартным ставкам ГО
        /// </summary>
        public int GoCoeff { get; set; }

        /// <summary>
        /// Запрет на заявки buy-stop и sell-stop
        /// </summary>
        public bool BsStopDeny { get; set; }

        /// <summary>
        /// Настройка запрета на противоположные торговые операции в инструменте после совершения сделки
        /// </summary>
        public string Retain { get; set; }

        /// <summary>
        /// Группа клиентов
        /// </summary>
        public ClientGroup GroupId { get; set; }

        #endregion

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} ({1})", Client, TradeCode);
        }

        /// <summary>
        /// ICloneable
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new Portfolio
            {
                AccountId = AccountId,
                Client = Client,
                TradeCode = TradeCode,
                CodeWord = CodeWord,
                Currency = Currency,
                MoneyInInit = MoneyInInit,
                MoneyInDay = MoneyInDay,
                MoneyOutInit = MoneyOutInit,
                MoneyOutDay = MoneyOutDay,
                Capital = Capital,
                CoverageFact = CoverageFact,
                PL = PL,
                Turnover = Turnover,
                TurnoverCurrencyCalc = TurnoverCurrencyCalc,
                Active = Active,
                Contragent = Contragent,
                OpenBalance = OpenBalance,
                FinRes = FinRes,
                PLCurrencyCalc = PLCurrencyCalc,
                UtilizationFact = UtilizationFact,
                MarginCall = MarginCall,
                IsRefund = IsRefund,
                IsMaxProfitExceed = IsMaxProfitExceed,
                IsMaxPercentProfitExceed = IsMaxPercentProfitExceed,
                IsMaxTurnoverExceed = IsMaxTurnoverExceed,
                IsMaxPercentTurnoverExceed = IsMaxPercentTurnoverExceed,
                CapitalCurrencyCalc = CapitalCurrencyCalc,
                CapitalCurrencyDisplay = CapitalCurrencyDisplay,
                PLCurrencyDisplay = PLCurrencyDisplay,
                TurnoverCurrencyDisplay = TurnoverCurrencyDisplay,
                ClientEmail = ClientEmail,
                AccessAuction = AccessAuction,
                BsStopDeny = BsStopDeny,
                GoCoeff = GoCoeff,
                Retain = Retain,
                MarginMin = MarginMin,
                GroupId = GroupId
            };
        }
    }

    /// <summary>
    /// Группы клиентов
    /// </summary>
    public enum ClientGroup
    {
        /// <summary>
        /// ММА
        /// </summary>
        MMA,

        /// <summary>
        /// ЗАО Финам
        /// </summary>
        ZAO
    }
}
