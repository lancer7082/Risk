using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Информация об объекте сервера
    /// </summary>
    [Serializable]
    public class ServerObjectInfo
    {
        public string Name { get; set; }
        public string ObjectType { get; set; }
    }
}