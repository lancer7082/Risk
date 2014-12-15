using System.Configuration;
using Finam.Configuration;
using Risk.Configuration.JobConfiguration;

namespace Risk.Configuration
{
    /// <summary>
    /// Секция настроек приложения сервера
    /// </summary>
    public class RiskServerSection : ConfigurationSection<Server>
    {
        private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Настройки сервера
        /// </summary>
        [ConfigurationProperty("server", IsRequired = true)]
        public ServerConfigurationElement Server
        {
            get { return this["server"] as ServerConfigurationElement; }
        }

        /// <summary>
        /// Джобы
        /// </summary>
        [ConfigurationProperty("jobs")]
        public JobTypesConfigurationElement Jobs
        {
            get
            {
                return this["jobs"] as JobTypesConfigurationElement;
            }
        }

        /// <summary>
        /// Расширения
        /// </summary>
        [ConfigurationProperty("addIns")]
        [ConfigurationCollection(typeof(AddinConfigurationElement))]
        public AddinConfigurationElementCollection<AddinConfigurationElement> AddIns
        {
            get
            {
                return this["addIns"] as AddinConfigurationElementCollection<AddinConfigurationElement>;
            }
        }

        /// <summary>
        /// Авторизация
        /// </summary>
        [ConfigurationProperty("authentications")]
        [ConfigurationCollection(typeof(AuthenticationConfigurationElement), AddItemName = "authentication")]
        public AuthenticationConfigurationElementCollection Authentications
        {
            get
            {
                return this["authentications"] as AuthenticationConfigurationElementCollection;
            }
        }

        /// <summary>
        /// Группы пользователей
        /// </summary>
        [ConfigurationProperty("usersToGroups")]
        [ConfigurationCollection(typeof(UserToGroupsConfigurationElement), AddItemName = "userToGroup")]
        public UsersToGroupsConfigurationElementCollection UsersToGroups
        {
            get
            {
                return this["usersToGroups"] as UsersToGroupsConfigurationElementCollection;
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

            if (Server != null)
                Server.ApplyConfigToObject(server);
            if (Jobs != null)
                Jobs.ApplyConfig(server);
            if (AddIns != null)
                AddIns.ApplyConfig(server);
            if (Authentications!=null)
                Authentications.ApplyConfig(server);
            _log.Info("Server configuration loaded");
        }

        /// <summary>
        /// UpdateConfig
        /// </summary>
        /// <param name="objectToConfigure"></param>
        public override void UpdateConfigFromObject(Server objectToConfigure)
        {
            if (objectToConfigure == null)
                return;

            if (Server != null)
                Server.UpdateConfigFromObject(objectToConfigure);
            if (Jobs != null)
                Jobs.UpdateConfig(objectToConfigure);
            if (AddIns!=null)
                AddIns.UpdateConfig(objectToConfigure);
            _log.Info("Server configuration saved");
        }
    }
}