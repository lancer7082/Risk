using System.Runtime.InteropServices;

namespace Risk
{
    /// <summary>
    /// Внешний интерфейс для подключения
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExportConnection
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string ServerName();

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string UserName();

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string Version();

        void Connect([MarshalAs(UnmanagedType.LPWStr)]string connectionString);
        void Disconnect();

        [return: MarshalAs(UnmanagedType.Interface)]
        IExportCommand CreateCommand(int instanceId);

        [return: MarshalAs(UnmanagedType.Interface)]
        IExportDataSet CreateDataSet(int instanceId);

        void OnDisconnect([MarshalAs(UnmanagedType.FunctionPtr)]DisconnectEvent disconnectEvent);
        void OnStateChanged([MarshalAs(UnmanagedType.FunctionPtr)]ConnectStateEvent connectStateEvent);
        void OnMessage([MarshalAs(UnmanagedType.FunctionPtr)]MessageEvent messageEvent);
        void OnCommand([MarshalAs(UnmanagedType.FunctionPtr)]CommandEvent commandEvent);
    }

    public delegate void DisconnectEvent(int instatnceId);
    public delegate void ConnectStateEvent(int instatnceId, int state);
    public delegate void CommandEvent(int instatnceId, IExportCommand command);
    public delegate void MessageEvent(int instatnceId, [MarshalAs(UnmanagedType.LPWStr)]string message, int messageType);
}