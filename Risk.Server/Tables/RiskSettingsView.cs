using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Опции
    /// </summary>
    [DataObject("Settings")]
    public class RiskSettingsView : DataObjectView<RiskSettings>
    {
        public RiskSettingsView()
            : base(Server.Settings)
        {
        }

        public override void SetData(ParameterCollection parameters, object data)
        {
            base.SetData(parameters, data);
            Server.Current.DataBase.WriteSettings(Data);
        }
    }
}