using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class ExportParameters : IExportParameters
    {
        private ParameterCollection _parameters;

        public ExportParameters(ParameterCollection parameters)
        {
            _parameters = parameters;
        }

        public void Clear()
        {
            _parameters.Clear();
        }

        public int GetParamCount()
        {
            return _parameters.Count;
        }

        public string GetParamName(int index)
        {
            return _parameters.GetParameter(index).Name;
        }

        public object GetParam(string name)
        {
            return _parameters[name];
        }

        public void SetParam(string name, object value)
        {
            _parameters[name] = value;
        }
    }
}