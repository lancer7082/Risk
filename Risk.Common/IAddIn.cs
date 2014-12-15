using System;

namespace Risk
{
    /// <summary>
    /// Расширение приложения
    /// </summary>
    public interface IAddIn
    {
        string Name();
        string Version();
        void Start(IServer server);
        void Stop();
        object Execute(Command command);
        void Configure(string configuration);
        string GetConfiguration();
    }
}