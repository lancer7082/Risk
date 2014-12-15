using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Finam.NativeConnect
{
    /// <summary>
    /// Внешний интерфейс для набора данных
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExportDataSet
    {
        // Records
        int RecordCount();

        int GetRecordIndex(int bookmark);

        [return: MarshalAs(UnmanagedType.SafeArray)]
        object[] GetRecordData(int recordIndex);

        // Parameters
        [return: MarshalAs(UnmanagedType.Interface)]
        IExportParameters GetParameters();

        // Fields
        int FieldCount();

        [return: MarshalAs(UnmanagedType.Struct)]
        ExportFieldInfo GetField(int index);

        [return: MarshalAs(UnmanagedType.Interface)]
        IExportRecordset GetFieldAsData(int fieldIndex, int recordIndex);

        // Text
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetText();
        void SetText([MarshalAs(UnmanagedType.LPWStr)]string text);

        // Filter
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetFilter();
        void SetFilter([MarshalAs(UnmanagedType.LPWStr)]string filter);

        // Notifiaction
        [return: MarshalAs(UnmanagedType.I1)]
        bool GetNotifications();
        void SetNotifications([MarshalAs(UnmanagedType.I1)]bool notifications);

        // Open / Close
        void Open();
        void Close();

        // Events
        void OnDataUpdate([MarshalAs(UnmanagedType.FunctionPtr)]DataUpdateEvent dataUpdateEvent);
    }

    public delegate void DataUpdateEvent(int instatnceId, int dataUpdateType, /* [MarshalAs(UnmanagedType.LPArray)]*/ int[] items);
}