using System;

namespace Finam.NativeConnect
{
    /// <summary>
    /// Статус подключения
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// Подключение закрыто
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Активное подключение
        /// </summary>
        Active = 1,

        /// <summary>
        /// Выполняется попытка восстановления подключения
        /// </summary>
        Reconnecting = 2
    }
}