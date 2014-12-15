using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    [Table("TestTable", KeyFields = "Id")]
    public class TestTable : Table<TestTableItem>
    {
    }

    [TableResult("TestTableResultInfo", KeyFields = "Id", ResultKeyFields = "Id1")]
    public class TestTableResultInfo : Table<TestTableItem, TestTableItemInfo>
    {
    }
}