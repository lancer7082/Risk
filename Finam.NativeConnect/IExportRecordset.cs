using System.Runtime.InteropServices;

namespace Finam.NativeConnect
{
    /// <summary>
    /// Внешний интерфейс для набора строк
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExportRecordset
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
    }
}