using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ICommand
    {
        // Text
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetText();
        void SetText([MarshalAs(UnmanagedType.LPWStr)]string text);

        // Notifiaction
        byte GetNotification();
        void SetNotification(byte notification);

        void OnReceive([MarshalAs(UnmanagedType.FunctionPtr)]ReceiveEvent receiveEvent);

        // Records

        int RecordCount();
        void First();
        bool EOF();
        bool Next();

        // Fields

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetFieldNames();

        int GetFieldType([MarshalAs(UnmanagedType.LPWStr)]string fieldName);

        int GetFieldAsInteger([MarshalAs(UnmanagedType.LPWStr)]string fieldName);

        bool GetFieldAsBoolean([MarshalAs(UnmanagedType.LPWStr)]string fieldName);

        double GetFieldAsDouble([MarshalAs(UnmanagedType.LPWStr)]string fieldName);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetFieldAsString([MarshalAs(UnmanagedType.LPWStr)]string fieldName);

        long GetFieldAsLong([MarshalAs(UnmanagedType.LPWStr)]string fieldName);

        double GetFieldAsDateTime([MarshalAs(UnmanagedType.LPWStr)]string fieldName);
    }
}
