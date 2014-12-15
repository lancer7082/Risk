using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration
{
    /// <summary>
    /// Группа пользователей
    /// </summary>
    public class UserToGroupsConfigurationElement : ConfigurationElement<Server>
    {
        /// <summary>
        /// UserName
        /// </summary>
        [ConfigurationProperty("userName", IsRequired = true)]
        public string UserName
        {
            get { return (string)this["userName"]; }
            set { this["userName"] = value; }
        }

        /// <summary>
        /// GroupId
        /// </summary>
        [ConfigurationProperty("groupId", IsRequired = true)]
        public int GroupId
        {
            get { return (int)this["groupId"]; }
            set { this["groupId"] = value; }
        }

        #region Overrides of ObjectConfigurationElement<Server>

        public override void ApplyConfigToObject(Server objectToConfigure)
        {
            base.ApplyConfigToObject(objectToConfigure);
        }
        public override void UpdateConfigFromObject(Server objectToConfigure)
        {
            base.UpdateConfigFromObject(objectToConfigure);
        }
        #endregion
    }
}
