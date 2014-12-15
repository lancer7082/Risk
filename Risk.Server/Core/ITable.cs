using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public interface ITable : IDataObject
    {
        string KeyFieldNames { get; }

        void Insert(object data);
        void Update(object data, string fieldNames);
        void Delete(object data);
        void Merge(object data, string fieldsNames, string keyFieldNames);
    }
}
