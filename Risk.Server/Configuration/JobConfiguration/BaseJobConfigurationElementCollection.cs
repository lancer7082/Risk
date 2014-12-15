using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration.JobConfiguration
{
    /// <summary>
    /// Коллекция элементов конфигурации джобов
    /// </summary>
    /// <typeparam name="TJobConfigurationElement"></typeparam>
    public class BaseJobConfigurationElementCollection<TJobConfigurationElement> : ConfigurationElementCollection<TJobConfigurationElement>
        where TJobConfigurationElement : BaseJobConfigurationElement, new()
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TJobConfigurationElement)element).Name;
        }

        /// <summary>
        /// Применяет конфиг к серверу
        /// </summary>
        /// <param name="server"></param>
        public void ApplyConfig(Server server)
        {
            foreach (BaseJobConfigurationElement item in this)
            {
                item.ApplyConfigToObject(server);
            }
        }

        public void UpdateConfig(Server server)
        {
            foreach (BaseJobConfigurationElement item in this)
            {
                item.UpdateConfigFromObject(server);
            }
        }
    }
}
