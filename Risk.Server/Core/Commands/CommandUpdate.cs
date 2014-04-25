using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Обновление строк таблицы
    /// </summary>
    [Command("Update")]
    public class CommandUpdate : CommandTable
    {
        /// <summary>
        /// Поля для обновления
        /// <remarks>Если имя поля начинается с '@', то для поля будет выполнено накопительное обновление. Поддерживаются не все типы полей</remarks>
        /// </summary>
        public string Fields 
        {
            get { return (string)Parameters["Fields"]; }
            set { Parameters["Fields"] = value; }
        }

        protected internal override void InternalExecute()
        {
            if (Object == null)
                throw new Exception("Object name is empty");

            if (Data == null)
                Data = Object.CreateDataFromParams(Parameters, true);

            Object.SetData(Parameters, Data);
        }
    }
}