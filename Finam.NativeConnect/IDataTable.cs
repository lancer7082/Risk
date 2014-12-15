using System.Collections;
using Risk;

namespace Finam.NativeConnect
{
    public interface IDataTable
    {
        int RecordCount();
        int GetRecordIndex(int bookmark);
        object[] GetRecordData(int recordIndex);
        object GetFieldData(int fieldIndex, int recordIndex);

        int FieldCount();
        ExportFieldInfo GetField(int fieldIndex);

        int[] UpdateData(DataUpdateType dataUpdateType, FieldInfo[] fieldsInfo, IEnumerable items);
    }
}
