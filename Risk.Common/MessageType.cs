using System;

namespace Risk
{
    /// <summary>
    /// Тип сообщения
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Отладочное сообщение
        /// </summary>
        Trace = 0,

        /// <summary>
        /// Информационное сообщение
        /// </summary>
        Info = 1,

        /// <summary>
        /// Предупредительное сообщение
        /// </summary>
        Warning = 2, 

        /// <summary>
        /// Ошибка
        /// </summary>
        Error = 3,
    }
}
