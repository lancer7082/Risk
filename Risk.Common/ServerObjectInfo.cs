using System;

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