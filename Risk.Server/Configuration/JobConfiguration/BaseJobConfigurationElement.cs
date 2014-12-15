using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration.JobConfiguration
{
    /// <summary>
    /// Конфигурация базового джоба
    /// </summary>
    public class BaseJobConfigurationElement : ConfigurationElement<Server>
    {

        // значения полей по умолчанию
        private const bool DefaultEnabledValue = true;

        private const bool DefaultAutoStartValue = true;

        private const int DefaultPeriodValue = 0;

        /// <summary>
        /// Имя
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        /// <summary>
        /// Период выполнения джоба
        /// </summary>
        [ConfigurationProperty("period", IsRequired = false, DefaultValue = DefaultPeriodValue)]
        public int Period
        {
            get { return (int)this["period"]; }
            set { this["period"] = value; }
        }

        /// <summary>
        /// Enabled джоба
        /// </summary>
        [ConfigurationProperty("enabled", IsRequired = false, DefaultValue = DefaultEnabledValue)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        /// <summary>
        /// AutoStart джоба
        /// </summary>
        [ConfigurationProperty("autoStart", IsRequired = false, DefaultValue = DefaultAutoStartValue)]
        public bool AutoStart
        {
            get { return (bool)this["autoStart"]; }
            set { this["autoStart"] = value; }
        }

        /// <summary>
        /// Применяет конфиг к серверу
        /// </summary>
        /// <param name="server"></param>
        public override void ApplyConfigToObject(Server server)
        {
        }

        /// <summary>
        /// Обновление конфига из объекта сервера
        /// </summary>
        /// <param name="server"></param>
        public override void UpdateConfigFromObject(Server server)
        {
            var job = server.JobManager.GetJobByName(Name);
            if (job == null)
            {
                Enabled = false;
                return;
            }

            // проверяем, что значения объекта не совпадают со значениями полей по умолчанию,
            // чтобы не затереть дефолтные значения в конфиге (т.е. этих настроек там нет)

            if (!(Enabled == DefaultEnabledValue && job.Enabled == DefaultEnabledValue))
                Enabled = job.Enabled;

            if (!(Period == DefaultPeriodValue && job.Period == DefaultPeriodValue))
                Period = job.Period;

            if (!(AutoStart == DefaultAutoStartValue && job.AutoStart == DefaultAutoStartValue))
                AutoStart = job.AutoStart;
        }
    }
}