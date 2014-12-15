using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration
{
    /// <summary>
    /// Коллекция элементов конфигурации авторизации
    /// </summary>
    public class AuthenticationConfigurationElementCollection : DatabaseConfigurationElementCollection<AuthenticationConfigurationElement>
    {
        /// <summary>
        /// Ключ элемента
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AuthenticationConfigurationElement)element).UserName;
        }

        /// <summary>
        /// Применяет конфиг к серверу
        /// </summary>
        /// <param name="server"></param>
        public void ApplyConfig(Server server)
        {
            foreach (AuthenticationConfigurationElement item in this)
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
            foreach (AuthenticationConfigurationElement item in this)
            {
                item.UpdateConfigFromObject(server);
            }
        }
    }
}
