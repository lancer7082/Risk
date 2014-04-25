using System;

namespace Risk
{
    /// <summary>
    /// Информация о подключении к серверу
    /// </summary>
    [Serializable]
    public class ConnectionInfo : ICloneable
    {
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public DateTime StartTime { get; set; }

        public object Clone()
        {
            return new ConnectionInfo
            {
                ConnectionId = this.ConnectionId,
                UserName = this.UserName,
                Address = this.Address,
                Port = this.Port,
                StartTime = this.StartTime,
            };
        }
    }
}