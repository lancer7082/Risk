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
        public decimal GORate
        {
            //•	Ставка ГО Long Клиента для Инструмента – для расчета размера положительной позиции к закрытию;
            //•	Ставка ГО Short Клиента для Инструмента - для расчета размера отрицательной позиции к закрытию;
            get
            {
                return Balance > 0 ? GORateLong : GORateShort;
            }
        }

        /// <summary>
        /// Ставка ГО клиента для инструмента, .00 %%
        /// m_dGORate
        /// </summary>
        public decimal GORateLong { get; set; }

        /// <summary>
        /// Ставка ГО клиента для инструмента, .00 %%
        /// m_dGORate
        /// </summary>
        public decimal GORateShort { get; set; }

        /// <summary>
        /// ГО позиции
        /// m_dGOPos
        /// </summary>
        public decimal GOPos { get; set; }

        /// <summary>
        /// Ставка ГО по инструменту
        /// </summary>
        public decimal InstrumentGORate { get; set; }

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
        /// Торговый оборот в валюте расчетов
        /// <remarks>Правила расчета см. в ФТ 2</remarks>
        /// </summary>
        public decimal TurnoverCurrencyCalc { get; set; }

        /// <summary>
        /// Торговый оборот в единицах актива -  сумма по показателю «Количество» в сделках по инструменту в текущей торговой сессии
        /// </summary>
        public decimal TurnoverQuantity { get; set; }

        /// <summary>
        /// Если true - Счет центрального контрагента
        /// </summary>
        public bool Contragent { get; set; }

        /// <summary>
        /// Входящее сальдо, шт.
        /// </summary>
        public int OpenBalance { get; set; }

        /// <summary>
        /// P\L в валюте расчетов
        /// </summary>
        public decimal PLCurrencyCalc { get; set; }

        /// <summary>
        /// итоговый результат на начало сессии
        /// </summary>
        public decimal FinRes { get; set; }

        /// <summary>
        /// Название инструмента
        /// </summary>
        public string InstrumentName { get; set; }

        /// <summary>
        /// Код класса инструмента
        /// </summary>
        public string InstrumentClassCode { get; set; }

        /// <summary>
        /// Имя класса инструмента
        /// </summary>
        public string InstrumentClassName { get; set; }

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
        /// итоговый результат в валюте отображения
        /// </summary>
        public decimal FinResCurrencyDisplay { get; set; }

        /// <summary>
        /// Котировка инструмента в валюте портфеля
        /// </summary>
        public decimal Quote { get; set; }

        /// <summary>
        /// Кросс курс инстурмента к валюте портфеля
        /// </summary>
        public decimal CrossRate { get; set; }

        /// <summary>
        /// Cтоимость позиции в валюте портфеля
        /// </summary>
        public decimal PositionCost { get; set; }

        /// <summary>
        /// ICloneable
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new Position
            {
                AccountId = AccountId,
                TradeCode = TradeCode,
                SecurityCurrency = SecurityCurrency,
                SecCode = SecCode,
                OpenBalance = OpenBalance,
                Bought = Bought,
                Sold = Sold,
                Balance = Balance,
                PL = PL,
                PLCurrencyCalc = PLCurrencyCalc,
                GOPos = GOPos,
                Contragent = Contragent,
                // Из сделок
                DealsCount = DealsCount,
                Turnover = Turnover,
                TurnoverCurrencyCalc = TurnoverCurrencyCalc,
                TurnoverQuantity = TurnoverQuantity,
                FinRes = FinRes,
                InstrumentClassName = InstrumentClassName,
                InstrumentClassCode = InstrumentClassCode,
                InstrumentName = InstrumentName,
                PLCurrencyDisplay = PLCurrencyDisplay,
                TurnoverCurrencyDisplay = TurnoverCurrencyDisplay,
                FinResCurrencyDisplay = FinResCurrencyDisplay,
                Quote = Quote,
                CrossRate = CrossRate,
                GORateLong = GORateLong,
                GORateShort = GORateShort,
                InstrumentGORate = InstrumentGORate,
                PositionCost = PositionCost
            };
        }
    }
}
