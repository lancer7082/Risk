using System;
using System.Runtime.Serialization;

namespace Risk
{
    /// <summary>
    /// Клиент
    /// </summary>
    public class Client : ICloneable // TODO: ??? INotifyPropertyChanged
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public double Balance { get; set; }

        public string Test { get; set; }

        public TimeSpan UpdateTime { get; set; }

        public bool Updated { get; set; } // TODO: ??? Переделать на поле-маску для определения изменившихся полей

        public object Clone()
        {
            return new Client
            {
                Id = this.Id,
                Name = this.Name,
                Balance = this.Balance,
                Test = this.Test,
                UpdateTime = this.UpdateTime,
                Updated = this.Updated,
            };
        }
    }
}
