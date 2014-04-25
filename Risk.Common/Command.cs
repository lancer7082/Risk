using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Risk
{
    /// <summary>
    /// Команда
    /// </summary>
    [Serializable]
    [DataContract]
    [KnownType(typeof(Parameter))]
    [KnownType(typeof(Rate[]))]
    [KnownType(typeof(Alert[]))]
    [KnownType(typeof(PortfolioRule[]))]
    [KnownType(typeof(Client[]))]
    [KnownType(typeof(Portfolio[]))]
    [KnownType(typeof(Position[]))]
    [KnownType(typeof(Trade[]))]
    [KnownType(typeof(Order[]))]
    [KnownType(typeof(PortfolioRuleInfo[]))]
    [KnownType(typeof(AlertInfo[]))]
    [KnownType(typeof(ConnectionInfo[]))]
    [KnownType(typeof(RiskSettings))]
    [KnownType(typeof(HierarchyObject[]))]
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
        public object Data { get; set; }

        private int CommandDataRowCount()
        {
            if (Data == null)
                return 0;

            if (Data is Array)
                return ((Array)Data).Length;

            return 0;
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