using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration
{
    /// <summary>
    /// Коллекция элементов конфигурации расширений
    /// </summary>
    /// <typeparam name="TAddinConfigurationElement"></typeparam>
    public class AddinConfigurationElementCollection<TAddinConfigurationElement> : ConfigurationElementCollection<TAddinConfigurationElement>
        where TAddinConfigurationElement : AddinConfigurationElement, new()
    {
        #region Overrides of ConfigurationElementCollection

        /// <summary>
        /// Gets the type of the <see cref="T:System.Configuration.ConfigurationElementCollection"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="T:System.Configuration.ConfigurationElementCollectionType"/> of this collection.
        /// </returns>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        /// <summary>
        /// Gets the name used to identify this collection of elements in the configuration file when overridden in a derived class.
        /// </summary>
        /// <returns>
        /// The name of the collection; otherwise, an empty string. The default is an empty string.
        /// </returns>
        protected override string ElementName
        {
            get { return "addIn"; }
        }

        #endregion

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((TAddinConfigurationElement)element).Name;
        }

        /// <summary>
        /// Применяет конфиг к серверу
        /// </summary>
        /// <param name="server"></param>
        public void ApplyConfig(Server server)
        {
            foreach (AddinConfigurationElement item in this)
            {
                item.ApplyConfigToObject(server);
            }
        }

        public void UpdateConfig(Server server)
        {
            foreach (AddinConfigurationElement item in this)
            {
                item.UpdateConfigFromObject(server);
            }
        }
    }
}
