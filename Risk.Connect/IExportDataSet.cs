using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Внешний интерфейс для набора данных
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExportDataSet
    {
        // Text
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetObjectName();
        void SetObjectName([MarshalAs(UnmanagedType.LPWStr)]string text);

        // Params
        [return: MarshalAs(UnmanagedType.Interface)]
        IExportParameters GetParameters();

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

        // Data
        [return: MarshalAs(UnmanagedType.Interface)]
        IExportData GetData();

        // Events
        void OnDataUpdate([MarshalAs(UnmanagedType.FunctionPtr)]DataUpdateEvent dataUpdateEvent);
    }

    public delegate void DataUpdateEvent(int instatnceId, int dataUpdateType);
}
