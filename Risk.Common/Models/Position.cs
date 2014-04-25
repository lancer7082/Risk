using System;

namespace Risk
{
    /// <summary>
    /// Позиция
    /// </summary>
    [Serializable]
    public class Position : ICloneable
    {
        /// <summary>
        /// Id счета
        /// </summary>
        public int AccountId { get; set; }

        /// <summary>
        /// Код счета
        /// </summary>
        public string TradeCode { get; set; }

        /// <summary>
        /// Валюта инструмента
        /// </summary>
        public string SecurityCurrency { get; set; }

        /// <summary>
        /// Код инструмента
        /// </summary>
        public string SecCode { get; set; }

        /// <summary>
        /// Купленный объем – сумма положительных открытых позиций по инструменту по всем клиентским счетам (торговым кодам)
        /// </summary>
        public int Bought { get; set; }

        /// <summary>
        /// Проданный объем – сумма отрицательных открытых позиций по инструменту по всем клиентским счетам (торговым кодам)
        /// </summary>
        public int Sold { get; set; }

        /// <summary>
        /// Общий объем – нетто позиция по инструменту: Купленный объем – Проданный объем
        /// </summary>
        public int Balance { get; set; }

        /// <summary>
        /// P\L – сумма по показателю «P\L» по всем клиентским счетам (торговым кодам) в разрезе инструмента 
        /// Приб/убыт по входящ +  Приб/убыт по сделкам
        /// m_dPLInit + m_dPLDay        
        /// </summary>
        public decimal PL { get; set; }

        /// <summary>
        /// Ставка ГО клиента для инструмента, .00 %%
        /// m_dGORate
        /// </summary>
        public decimal GORate { get; set; }
        
        /// <summary>
        /// ГО позиции
        /// m_dGOPos
        /// </summary>
        public decimal GOPos { get; set; }

        #region  Рассчитываемые значения

        /// <summary>
        /// Сделки – количество сделок по инструменту в текущей торговой сессии
        /// </summary>
        public int DealsCount { get; set; }

        /// <summary>
        /// Торговый оборот - торговый оборот в текущей торговой сессии на основании совершенных сделок по инструменту
        /// <remarks>Правила расчета см. в ФТ 2</remarks> 
        /// </summary>
        public decimal Turnover { get; set; }

        /// <summary>
        /// Торговый оборот в USD – торговый оборот в USD в текущей торговой сессии на основании совершенных сделок по инструменту
        /// <remarks>Правила расчета см. в ФТ 2</remarks>
        /// </summary>
        public decimal TurnoverCurrencyCalc { get; set; }

        /// <summary>
        /// Торговый оборот в единицах актива -  сумма по показателю «Количество» в сделках по инструменту в текущей торговой сессии
        /// </summary>
        public decimal TurnoverQuantity { get; set; }

        #endregion

        /// <summary>
        /// Если true - Счет центрального контрагента
        /// </summary>
        public bool Contragent { get; set; }

        public object Clone()
        {
            return new Position
            {
                AccountId = this.AccountId,
                TradeCode = this.TradeCode,
                SecurityCurrency = this.SecurityCurrency,
                SecCode = this.SecCode,
                Bought = this.Bought,
                Sold = this.Sold,
                Balance = this.Balance,
                PL = this.PL,
                GORate = this.GORate,
                GOPos = this.GOPos,
                Contragent = this.Contragent,
                // Из сделок
                DealsCount = this.DealsCount,
                Turnover = this.Turnover,
                TurnoverCurrencyCalc = this.TurnoverCurrencyCalc,
                TurnoverQuantity = this.TurnoverQuantity,
            };
        }
    }
}
