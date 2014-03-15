using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [Serializable]
    public class Portfolio // TODO: ??? : NotifyData, IExtensibleDataObject, INotifyPropertyChanged
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
        /// Баланс – капитал клиента с учетом нереализованного P\L
        /// </summary>
        public double Capital { get; set; }

        /// <summary>
        /// Обеспеченность
        /// </summary>
        public double Coverage { get; set; }

        /// <summary>
        /// P\L – общая прибыль или убыток
        /// </summary>
        public double PL { get; set; }

        /// <summary>
        /// Оборот (USD) – рассчитанный торговый оборот по клиенту \ инструменту
        /// </summary>
        public decimal Turnover { get; set; }

        /// <summary>
        /// Торги доступны
        /// </summary>
        public bool Active { get; set; }

        public override string ToString()
        {
            return String.Format("{0} ({1})", Client, TradeCode);
        }
    }
}
