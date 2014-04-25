using System;

namespace Risk
{
    /// <summary>
    /// Расширение приложения
    /// </summary>
    public interface IAddIn
    {
        // TODO: !!! void Configure(Configuration)
        string Name();
        string Version();
        void Start(IServer server);
        void Stop();
        void Execute(Command command);
    }
}