using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExportCommand
    {
        // Command Text
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetCommandText();
        void SetCommandText([MarshalAs(UnmanagedType.LPWStr)]string commandText);

        // Parameters
        [return: MarshalAs(UnmanagedType.Interface)]
        IExportParameters GetParameters();

        // Data
        [return: MarshalAs(UnmanagedType.Interface)]
        IExportData GetData();

        // Execute
        void Execute();
    }
}