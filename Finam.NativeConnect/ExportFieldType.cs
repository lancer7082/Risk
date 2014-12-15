using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finam.NativeConnect
{
    public enum ExportFieldType
    {
        ftUnknown = 0x00,
        ftString = 0x01,
        ftSmallint = 0x02,
        ftInteger = 0x03,
        ftWord = 0x04,
        ftBoolean = 0x05,
        ftFloat = 0x06,
        ftCurrency = 0x07,
        ftBCD = 0x08,
        ftDate = 0x09,
        ftTime = 0x0A,
        ftDateTime = 0x0B,
        ftBytes = 0x0C,
        ftVarBytes = 0x0D,
        ftAutoInc = 0x0E,
        ftBlob = 0x0F,
        ftMemo = 0x10,
        ftGraphic = 0x11,
        ftFmtMemo = 0x12,
        ftParadoxOle = 0x13,
        ftDBaseOle = 0x14,
        ftTypedBinary = 0x15,
        ftCursor = 0x16,
        ftFixedChar = 0x17,
        ftWideString = 0x18,
        ftLargeint = 0x19,
        ftADT = 0x1A,
        ftArray = 0x1B,
        ftReference = 0x1C,
        ftDataSet = 0x1D,
        ftOraBlob = 0x1E,
        ftOraClob = 0x1F,
        ftVariant = 0x20,
        ftInterface = 0x21,
        ftIDispatch = 0x22,
        ftGuid = 0x23,
        ftTimeStamp = 0x24,
        ftFMTBcd = 0x25,
        ftFixedWideChar = 0x26,
        ftWideMemo = 0x27,
        ftOraTimeStamp = 0x28,
        ftOraInterval = 0x29,
        ftLongWord = 0x2A,
        ftShortint = 0x2B,
        ftByte = 0x2C,
        ftExtended = 0x2D,
        ftConnection = 0x2E,
        ftParams = 0x2F,
        ftStream = 0x30,
        ftTimeStampOffset = 0x31,
        ftObject = 0x32,
        ftSingle = 0x33
    }
}
