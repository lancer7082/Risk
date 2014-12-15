using System;
using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration
{
    /// <summary>
    /// Секция настроек сервера
    /// </summary>
    public sealed class ServerConfigurationElement : ConfigurationElement<Server>
    {
        // значения полей по умолчанию
        private readonly string _defaultServerName = Environment.MachineName;

        private readonly int _defaultProcessCount = Environment.ProcessorCount * 2;

        private const int DefaultRefreshTime = 1000;

        private const int DefaultAutoMarginCallInterval = 10000;

        private const int DatabaseCommandTimeoutDefaultValue = 300;


        /// <summary>
        /// Имя
        /// </summary>
        [ConfigurationProperty("name")]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        /// <summary>
        /// FirmId
        /// </summary>
        [ConfigurationProperty("firmId", IsRequired = false)]
        public byte? FirmId
        {
            get
            {
                return this["firmId"] as byte?;
            }
            set
            {
                this["firmId"] = value;
            }
        }

        /// <summary>
        /// ConnectionStringName
        /// </summary>
        [ConfigurationProperty("connectionStringName", IsRequired = false)]
        public string ConnectionStringName
        {
            get
            {
                return this["connectionStringName"] as string;
            }
            set
            {
                this["connectionStringName"] = value;
            }
        }

        /// <summary>
        /// Таймаут команды
        /// </summary>
        [ConfigurationProperty("databaseCommandTimeout", IsRequired = false, DefaultValue = DatabaseCommandTimeoutDefaultValue)]
        public int DatabaseCommandTimeout
        {
            get
            {
                return (int)this["databaseCommandTimeout"];
            }
            set
            {
                this["databaseCommandTimeout"] = value;
            }
        }

        /// <summary>
        /// Таймаут команды
        /// </summary>
        [ConfigurationProperty("processCount")]
        public int? ProcessCount
        {
            get
            {
                return (int?)this["processCount"];
            }
            set
            {
                this["processCount"] = value;
            }
        }

        /// <summary>
        /// Таймаут команды
        /// </summary>
        [ConfigurationProperty("refreshTime", DefaultValue = DefaultRefreshTime)]
        public int RefreshTime
        {
            get
            {
                return (int)this["refreshTime"];
            }
            set
            {
                this["refreshTime"] = value;
            }
        }

        /// <summary>
        /// AutoMarginCall Intreval
        /// </summary>
        [ConfigurationProperty("autoMarginCallInterval", DefaultValue = DefaultAutoMarginCallInterval)]
        public int AutoMarginCallInterval
        {
            get
            {
                return (int)this["autoMarginCallInterval"];
            }
            set
            {
                this["autoMarginCallInterval"] = value;
            }
        }

        /// <summary>
        /// ConnectionStringName
        /// </summary>
        [ConfigurationProperty("transaqUsaHostName", IsRequired = false)]
        public string TransaqUsaHostName
        {
            get
            {
                return this["transaqUsaHostName"] as string;
            }
            set
            {
                this["transaqUsaHostName"] = value;
            }
        }

        /// <summary>
        /// Применяет конфиг к серверу
        /// </summary>
        /// <param name="server"></param>
        public override void ApplyConfigToObject(Server server)
        {
            if (server == null)
                return;

            server.FirmId = FirmId;
            server.ConnectionString = ConnectionStringName.GetConnectionStringByName();
            server.DatabaseCommandTimeout = DatabaseCommandTimeout;
            server.TransaqUsaHostName = TransaqUsaHostName;

            server.ServerName = string.IsNullOrEmpty(Name) ? _defaultServerName : Name;
            server.ProcessCount = ProcessCount ?? _defaultProcessCount;
            server.RefreshTime = RefreshTime;
            server.AutoMarginCallInterval = AutoMarginCallInterval;
        }

        /// <summary>
        /// Обновляет конфиг из настроек сервера
        /// </summary>
        /// <param name="server"></param>
        public override void UpdateConfigFromObject(Server server)
        {
            if (server == null)
                return;

            FirmId = server.FirmId;
            TransaqUsaHostName = server.TransaqUsaHostName;

            // проверяем, что значения объекта не совпадают со значениями полей по умолчанию,
            // чтобы не затереть дефолтные значения в конфиге (т.е. этих настроек там нет)

            if (!(string.IsNullOrEmpty(Name) && server.ServerName == _defaultServerName))
                Name = server.ServerName;

            if (!(!ProcessCount.HasValue && server.ProcessCount == _defaultProcessCount))
                ProcessCount = server.ProcessCount;

            if (!(RefreshTime == DefaultRefreshTime && server.RefreshTime == DefaultRefreshTime))
                RefreshTime = server.RefreshTime;

            if (!(AutoMarginCallInterval == DefaultAutoMarginCallInterval && server.AutoMarginCallInterval == DefaultAutoMarginCallInterval))
                AutoMarginCallInterval = server.AutoMarginCallInterval;

            if (!(DatabaseCommandTimeout == DatabaseCommandTimeoutDefaultValue
                  && server.DatabaseCommandTimeout == DatabaseCommandTimeoutDefaultValue))
                DatabaseCommandTimeout = server.DatabaseCommandTimeout;
        }
    }
}