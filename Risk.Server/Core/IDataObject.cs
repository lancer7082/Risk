using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public interface IDataObject
    {
        string Name { get; }
        Type ObjectType { get; }
        int RefreshTime { get; set; }

        FieldInfo[] GetFields(ParameterCollection parameters);       
        object GetData(ParameterCollection parameters);
        void SetData(ParameterCollection parameters, object data);
        object CreateDataFromParams(ParameterCollection parameters, bool updateFields);

        void AddNotification(Connection connection, string correlationId, ParameterCollection parameters);
        void RemoveNotification(Connection connection, string correlationId);
    }
}
