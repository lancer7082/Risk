using System;

namespace Risk
{
    /// <summary>
    /// Инструмент
    /// </summary>
    [Serializable]
    public class Instrument : ICloneable
    {
        /// <summary>
        /// Код инструмента
        /// </summary>
        public string SecCode { get; set; }

        /// <summary>
        /// Валюта инструмента
        /// </summary>
        public string SecurityCurrency { get; set; }

        /// <summary>
        /// Кол-во знаков после запятой в цене
        /// </summary>
        public int Decimals { get; set; }

        /// <summary>
        /// Стоимость шага цены
        /// </summary>
        public int Bpcost { get; set; }

        /// <summary>
        /// Размер лота
        /// </summary>
        public int Lotsize { get; set; }

        /// <summary>
        /// Отступ котировок
        /// </summary>
        public int GShift { get; set; }

        /// <summary>
        /// Фильтрация потока входящих цен
        /// </summary>
        public decimal Sigma { get; set; }

        /// <summary>
        /// Количество повторений "далёких" цен за пределами sigma
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>
        /// Экспоненциальное сглаживание цен
        /// </summary>
        public decimal Smoothing { get; set; }

        /// <summary>
        /// Идентификатор группы инструмента
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Рыночные заявки разрешены (1-разрешены, 0- не разрешены)
        /// </summary>
        public bool MarketPermitted { get; set; }

        /// <summary>
        /// Разрешен к торгам (1-разрешен, 0- не разрешен)
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Покупка в кредит разрешена (1-разрешена, 0- не разрешена)
        /// </summary>
        public bool LongPermitted { get; set; }

        /// <summary>
        /// Инструмент разрешен к торгам для резидентов.
        /// </summary>
        public bool Resident { get; set; }

        /// <summary>
        /// Инструмент разрешен к торгам для нерезидентов.
        /// </summary>
        public bool NotResident { get; set; }

        /// <summary>
        /// Ставка гарантийного обеспечения (ГО). В сотых процента, допустимый диапазон значений - 1 – 10000 (что соответствует диапазону от 0.01% - 100%)
        /// </summary>
        public int GoCoeff { get; set; }

        /// <summary>
        /// Продажа в кредит разрешена (1-разрешена, 0- не разрешена)
        /// </summary>
        public bool ShortPermitted { get; set; }

        /// <summary>
        /// Запрет на заявки buy-stop и sell-stop (1-запрещены, 0- разрешены)
        /// </summary>
        public bool BS_StopDeny { get; set; }

        /// <summary>
        /// Запретная зона для заявок buy-stop и sell-stop, допустимые значения от 0.001 до 9.999
        /// </summary>
        public decimal BS_StopDenyZone { get; set; }

        /// <summary>
        /// Refund level инструмента
        /// </summary>
        /// <remarks>
        /// 0 - "рефандинга нет" 
        /// 1 - "рефандинг только для refund-клиентов" 
        /// 2 - "рефандинг для всех"
        /// </remarks>
        public int RefundLevel { get; set; }

        /// <summary>
        /// Внешний код инструмента в торговых системах (to_seccode)
        /// </summary>
        public string ExternalCode { get; set; }

        /// <summary>
        /// Рынок
        /// </summary>
        public int Market { get; set; }

        /// <summary>
        /// Имя инструмента
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Код класса инструмента
        /// </summary>
        public string ClassCode { get; set; }

        /// <summary>
        /// Имя класса инструмента
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Клонирование
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new Instrument
            {
                SecCode = SecCode,
                SecurityCurrency = SecurityCurrency,
                Decimals = Decimals,
                Bpcost = Bpcost,
                Lotsize = Lotsize,
                BS_StopDeny = BS_StopDeny,
                BS_StopDenyZone = BS_StopDenyZone,
                Enabled = Enabled,
                GoCoeff = GoCoeff,
                GroupId = GroupId,
                GShift = GShift,
                LongPermitted = LongPermitted,
                MarketPermitted = MarketPermitted,
                MaxCount = MaxCount,
                Resident = Resident,
                NotResident = NotResident,
                ShortPermitted = ShortPermitted,
                Sigma = Sigma,
                Smoothing = Smoothing,
                RefundLevel = RefundLevel,
                ExternalCode = ExternalCode,
                Market = Market,
                Name = Name,
                ClassCode = ClassCode,
                ClassName = ClassName
            };
        }
    }
}
