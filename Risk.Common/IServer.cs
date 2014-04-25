using System;
using System.IO;

namespace Risk
{
    /// <summary>
    /// Интерыейс сервера для расширений
    /// </summary>
    public interface IServer
    {
        T Execute<T>(Command command);
        object Execute(Command command);
        void ExecuteAsync(Command command);
    }
}