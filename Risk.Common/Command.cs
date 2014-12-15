using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Risk
{
    /// <summary>
    /// Команда
    /// </summary>
    [Serializable]
    [DataContract]
    [KnownType("KnownTypes")]
    public class Command
    {
        private ParameterCollection _parameters;

        [DataMember]
        public string CommandText { get; set; }

        [DataMember]
        public string CorrelationId { get; set; }

        [DataMember]
        public ParameterCollection Parameters
        {
            get 
            {
                if (_parameters == null)
                    _parameters = new ParameterCollection();
                return _parameters; 
            }

            set
            {
                if (_parameters != null)
                    _parameters.Clear();
                else
                    _parameters = new ParameterCollection();
                foreach (var param in value)
                {
                    _parameters.Add(param);
                }
            }
        }

        [DataMember]
        public FieldInfo[] FieldsInfo { get; set; }

        [DataMember]
        public object Data { get; set; }

        private int CommandDataRowCount()
        {
            if (Data == null)
                return 0;

            if (Data is Array)
                return ((Array)Data).Length;

            return 0;
        }

        private static IEnumerable<Type> _knownTypes;

        public static IEnumerable<Type> KnownTypes()
        {
            if (_knownTypes == null)
            {
                // TODO: ??? Check contract name unique
                var knownTypes = (from type in Assembly.GetExecutingAssembly().GetTypes()
                                  where type.IsSerializable
                                     || Attribute.GetCustomAttribute(type, typeof(DataContractAttribute)) != null
                                  select type).ToList();
                knownTypes.AddRange((from type in knownTypes
                                    select type.MakeArrayType()).ToArray());
                _knownTypes = knownTypes.ToArray();
            }
            return _knownTypes;
        }

        public override string ToString()
        {
            var result =  CommandText;
            if (Parameters.Contains("ObjectName"))
                result += " " + Parameters["ObjectName"];
            var rowCount = CommandDataRowCount();
            if (rowCount > 0)
                result += String.Format(" ({0} rows)", rowCount);
            return result;
        }

        public static Command Select(string objectName, string filter = null, ParameterCollection parameters = null)
        {
            var command = new Command();
            command.CommandText = "SELECT";
            if (parameters != null)
                command.Parameters = parameters;
            if (!String.IsNullOrWhiteSpace(objectName))
                command.Parameters["ObjectName"] = objectName;
            if (!String.IsNullOrWhiteSpace(filter))
                command.Parameters["Filter"] = filter;
            return command;
        }

        public static Command Insert(string objectName, object data)
        {
            var command = new Command();
            command.CommandText = "INSERT";
            if (!String.IsNullOrWhiteSpace(objectName))
              command.Parameters["ObjectName"] = objectName;
            command.Data = data;
            return command;
        }

        public static Command Update(string objectName, object data, string fields = null)
        {
            var command = new Command();
            command.CommandText = "UPDATE";
            if (!String.IsNullOrWhiteSpace(objectName))
              command.Parameters["ObjectName"] = objectName;
            if (!String.IsNullOrWhiteSpace(fields))
              command.Parameters["Fields"] = fields;
            command.Data = data;
            return command;
        }

        public static Command Merge(string objectName, object data, string fields = null, string keyFields = null)
        {
            var command = new Command();
            command.CommandText = "MERGE";
            if (!String.IsNullOrWhiteSpace(objectName))
              command.Parameters["ObjectName"] = objectName;
            if (!String.IsNullOrWhiteSpace(fields))
              command.Parameters["Fields"] = fields;
            if (!String.IsNullOrWhiteSpace(keyFields))
              command.Parameters["KeyFields"] = keyFields;
            command.Data = data;
            return command;
        }

        public static Command Delete(string objectName, object data)
        {
            var command = new Command();
            command.CommandText = "DELETE";
            if (!String.IsNullOrWhiteSpace(objectName))
                command.Parameters["ObjectName"] = objectName;
            command.Data = data;
            return command;
        }
    }
}