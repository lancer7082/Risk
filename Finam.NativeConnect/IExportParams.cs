using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Finam.NativeConnect
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExportParameters
    {
        void Clear();
        int GetParamCount();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetParamName(int index);
        object GetParam([MarshalAs(UnmanagedType.LPWStr)]string name);
        void SetParam([MarshalAs(UnmanagedType.LPWStr)]string name, object value);
    }
}
