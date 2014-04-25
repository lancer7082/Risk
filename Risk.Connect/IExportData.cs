using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExportData
    {
        // Records
        int RecordCount();
        void First();
        bool EOF();
        bool Next();

        // Fields
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetFieldNames();
        void SetFieldNames([MarshalAs(UnmanagedType.LPWStr)]string fieldNames);

        int GetFieldType([MarshalAs(UnmanagedType.LPWStr)]string fieldName);

        object GetFieldValue([MarshalAs(UnmanagedType.LPWStr)]string name);

        [return: MarshalAs(UnmanagedType.Interface)]
        IExportData GetFieldAsObject([MarshalAs(UnmanagedType.LPWStr)]string fieldName);

        double GetFieldAsDateTime([MarshalAs(UnmanagedType.LPWStr)]string fieldName);
    }
}
