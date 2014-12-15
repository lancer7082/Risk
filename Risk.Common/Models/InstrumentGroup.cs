using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Группа инструментов
    /// </summary>
    [Serializable]
    public class InstrumentGroup : ICloneable
    {
        /// <summary>
        /// Идентификатор группы
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Имя группы
        /// </summary>
        public string Name { get; set; }

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
        /// Клонирование
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return new InstrumentGroup
            {
                Id = Id,
                Name= Name,
                GShift = GShift,
                MaxCount = MaxCount,
                Sigma = Sigma,
                Smoothing = Smoothing
            };
        }
    }
}
