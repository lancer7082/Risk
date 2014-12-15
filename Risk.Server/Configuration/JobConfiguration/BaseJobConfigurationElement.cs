using System.Configuration;
using Finam.Configuration;

namespace Risk.Configuration.JobConfiguration
{
    /// <summary>
    /// ������������ �������� �����
    /// </summary>
    public class BaseJobConfigurationElement : ConfigurationElement<Server>
    {

        // �������� ����� �� ���������
        private const bool DefaultEnabledValue = true;

        private const bool DefaultAutoStartValue = true;

        private const int DefaultPeriodValue = 0;

        /// <summary>
        /// ���
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        /// <summary>
        /// ������ ���������� �����
        /// </summary>
        [ConfigurationProperty("period", IsRequired = false, DefaultValue = DefaultPeriodValue)]
        public int Period
        {
            get { return (int)this["period"]; }
            set { this["period"] = value; }
        }

        /// <summary>
        /// Enabled �����
        /// </summary>
        [ConfigurationProperty("enabled", IsRequired = false, DefaultValue = DefaultEnabledValue)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
            set { this["enabled"] = value; }
        }

        /// <summary>
        /// AutoStart �����
        /// </summary>
        [ConfigurationProperty("autoStart", IsRequired = false, DefaultValue = DefaultAutoStartValue)]
        public bool AutoStart
        {
            get { return (bool)this["autoStart"]; }
            set { this["autoStart"] = value; }
        }

        /// <summary>
        /// ��������� ������ � �������
        /// </summary>
        /// <param name="server"></param>
        public override void ApplyConfigToObject(Server server)
        {
        }

        /// <summary>
        /// ���������� ������� �� ������� �������
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

            // ���������, ��� �������� ������� �� ��������� �� ���������� ����� �� ���������,
            // ����� �� �������� ��������� �������� � ������� (�.�. ���� �������� ��� ���)

            if (!(Enabled == DefaultEnabledValue && job.Enabled == DefaultEnabledValue))
                Enabled = job.Enabled;

            if (!(Period == DefaultPeriodValue && job.Period == DefaultPeriodValue))
                Period = job.Period;

            if (!(AutoStart == DefaultAutoStartValue && job.AutoStart == DefaultAutoStartValue))
                AutoStart = job.AutoStart;
        }
    }
}