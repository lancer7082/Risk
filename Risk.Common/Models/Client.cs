using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class Client : IExtensibleDataObject // TODO: ??? INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public double Balance { get; set; }

        public string Test { get; set; }

        public TimeSpan TimeUpdate { get; set; }

        public bool Updated { get; set; } // TODO: ??? Переделать на поле-маску для определения изменившихся полей

        private ExtensionDataObject extensionDataObjectValue;
        public ExtensionDataObject ExtensionData
        {
            get
            {
                return extensionDataObjectValue;
            }
            set
            {
                extensionDataObjectValue = value;
            }
        }
    }
}
