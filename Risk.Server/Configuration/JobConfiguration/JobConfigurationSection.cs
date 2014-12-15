using System.Configuration;

namespace Risk.Configuration.JobConfiguration
{
    /// <summary>
    /// Секция конфигурации джобов
    /// </summary>
    public class JobConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("jobs")]
        public JobTypesConfigurationElement Jobs
        {
            get
            {
                return this["jobs"] as JobTypesConfigurationElement;
            }
        }
    }
}