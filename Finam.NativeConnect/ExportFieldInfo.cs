using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Finam.NativeConnect
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ExportFieldInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string FieldName;

        [MarshalAs(UnmanagedType.I4)]
        public ExportFieldType FieldType;

        [MarshalAs(UnmanagedType.I1)]
        public bool IsKey;

        public int Size;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Caption;

        [MarshalAs(UnmanagedType.I1)]
        public bool ReadOnly;

        [MarshalAs(UnmanagedType.I1)]
        public bool Hidden;
    }
}
