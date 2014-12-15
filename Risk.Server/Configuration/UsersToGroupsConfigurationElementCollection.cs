using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration
{
    /// <summary>
    /// Коллекция элементов групп пользователей
    /// </summary>
    public class UsersToGroupsConfigurationElementCollection : DatabaseConfigurationElementCollection<UserToGroupsConfigurationElement>
    {
        /// <summary>
        /// Ключ элемента
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((UserToGroupsConfigurationElement)element).UserName;
        }

        /// <summary>
        /// Применяет конфиг к серверу
        /// </summary>
        /// <param name="server"></param>
        public void ApplyConfig(Server server)
        {
            foreach (UserToGroupsConfigurationElement item in this)
            {
                item.ApplyConfigToObject(server);
            }
        }

        /// <summary>
        /// Обновляет конфиг из объекта сервера
        /// </summary>
        /// <param name="server"></param>
        public void UpdateConfig(Server server)
        {
            foreach (UserToGroupsConfigurationElement item in this)
            {
                item.UpdateConfigFromObject(server);
            }
        }
    }
}
