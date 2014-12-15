using System.Runtime.InteropServices;

namespace Finam.NativeConnect
{
    /// <summary>
    /// Внешний интерфейс для команды
    /// </summary>
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExportCommand
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

        // Command Text
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetText();
        void SetText([MarshalAs(UnmanagedType.LPWStr)]string commandText);

        // Execute
        void Execute();
    }
}