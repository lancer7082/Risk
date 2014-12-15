using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration
{
    /// <summary>
    /// Конфигурационный элемент авторизации
    /// </summary>
    public class AuthenticationConfigurationElement : ConfigurationElement<Server>
    {
        /// <summary>
        /// Login
        /// </summary>
        [ConfigurationProperty("userName", IsRequired = true)]
        public string UserName
        {
            get { return (string)this["userName"]; }
            set { this["userName"] = value; }
        }

        /// <summary>
        /// Password
        /// </summary>
        [ConfigurationProperty("passwordHash", IsRequired = true)]
        public string PasswordHash
        {
            get { return (string)this["passwordHash"]; }
            set { this["passwordHash"] = value; }
        }

        /// <summary>
        /// Password
        /// </summary>
        [ConfigurationProperty("webAccess", IsRequired = false, DefaultValue = false)]
        public bool WebAccess
        {
            get { return (bool)this["webAccess"]; }
            set { this["webAccess"] = value; }
        }
    }
}
