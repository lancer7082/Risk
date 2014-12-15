using System.Configuration;
using Finam.Configuration;
using Risk.Jobs;

namespace Risk.Configuration.JobConfiguration
{
    /// <summary>
    /// ������������ Database �����
    /// </summary>
    public class DatabaseJobConfigurationElement : BaseJobConfigurationElement
    {
        // �������� ����� �� ���������
        private const int DatabaseCommandTimeoutDefaultValue = 300;

        /// <summary>
        /// ConnectionStringName
        /// </summary>
        [ConfigurationProperty("connectionStringName", IsRequired = false)]
        public string ConnectionStringName
        {
            get
            {
                return this["connectionStringName"] as string;
            }
            set
            {
                this["connectionStringName"] = value;
            }
        }

        /// <summary>
        /// ������� �������
        /// </summary>
        [ConfigurationProperty("databaseCommandTimeout", IsRequired = false, DefaultValue = DatabaseCommandTimeoutDefaultValue)]
        public int DatabaseCommandTimeout
        {
            get
            {
                return (int)this["databaseCommandTimeout"];
            }
            set
            {
                this["databaseCommandTimeout"] = value;
            }
        }

        /// <summary>
        /// ��� ��������� �������
        /// </summary>
        [ConfigurationProperty("commandName", IsRequired = false)]
        public string CommandName
        {
            get { return this["commandName"] as string; }
            set { this["commandName"] = value; }
        }

        /// <summary>
        /// ��� ��������
        /// </summary>
        [ConfigurationProperty("storedProcedureName", IsRequired = false)]
        public string StoredProcedureName
        {
            get { return this["storedProcedureName"] as string; }
            set { this["storedProcedureName"] = value; }
        }
        /// <summary>
        /// ��� �������
        /// </summary>
        [ConfigurationProperty("dataObjectName", IsRequired = false)]
        public string DataObjectName
        {
            get { return this["dataObjectName"] as string; }
            set { this["dataObjectName"] = value; }
        }

        /// <summary>
        /// ���� �������
        /// </summary>
        [ConfigurationProperty("dataObjectFields", IsRequired = false)]
        public string DataObjectFields
        {
            get { return this["dataObjectFields"] as string; }
            set { this["dataObjectFields"] = value; }
        }

        /// <summary>
        /// ��������� ���������� ��������
        /// </summary>
        [ConfigurationProperty("storedProcedureParameters")]
        public KeyValueConfigurationCollection StoredProcedureParameters
        {
            get
            {
                return (KeyValueConfigurationCollection)base["storedProcedureParameters"];
            }
        }

        #region Overrides of BaseJobConfigurationElement

        /// <summary>
        /// ��������� ������ � �������
        /// </summary>
        /// <param name="server"></param>
        public override void ApplyConfigToObject(Server server)
        {

            base.ApplyConfigToObject(server);

            if (server == null || server.JobManager == null)
                return;

            var needRestart = false;
            var jobExists = false;

            var job = server.JobManager.GetJobByName(Name) as DatabaseJob;

            if (job == null)
            {
                job = new DatabaseJob();
            }
            else
            {
                jobExists = true;
                if (job.Period != Period || job.Enabled != Enabled || job.StoredProcedureName != StoredProcedureName)
                    needRestart = true;
            }

            job.IsConfiguring = true;

            lock (job.Locker)
            {
                job.Name = Name;
                job.ConnectionStringName = ConnectionStringName;
                job.ConnectionString = ConnectionStringName.GetConnectionStringByName();
                job.DatabaseCommandTimeout = DatabaseCommandTimeout;
                job.DataObjectName = DataObjectName;
                job.DataObjectFields = DataObjectFields;
                job.CommandName = CommandName;
                job.Period = Period;
                job.Enabled = Enabled;
                job.AutoStart = AutoStart;
                job.StoredProcedureName = StoredProcedureName;

                job.StoredProcedureParameters.Clear();

                // ��������� ��������� ��������
                foreach (KeyValueConfigurationElement parameter in StoredProcedureParameters)
                {
                    job.StoredProcedureParameters.Add(parameter.Key, parameter.Value);
                }
                job.IsConfiguring = false;

                // ��������� ���� � ��������
                if (!jobExists)
                {
                    server.JobManager.AddJob(job);
                }
                else if (needRestart)
                {
                    job.Restart();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        public override void UpdateConfigFromObject(Server server)
        {
            base.UpdateConfigFromObject(server);

            if (server == null || server.JobManager == null)
                return;
            var job = server.JobManager.GetJobByName(Name) as DatabaseJob;
            if (job == null)
                return;

            ConnectionStringName = job.ConnectionStringName;
            CommandName = job.CommandName;
            StoredProcedureName = job.StoredProcedureName;
            DataObjectName = job.DataObjectName;
            DataObjectFields = job.DataObjectFields;
            StoredProcedureParameters.Clear();
            foreach (var storedProcedureParameter in job.StoredProcedureParameters)
            {
                StoredProcedureParameters.Add(storedProcedureParameter.Key, storedProcedureParameter.Value.ToString());
            }

            // ���������, ��� �������� ������� �� ��������� �� ���������� ����� �� ���������,
            // ����� �� �������� ��������� �������� � ������� (�.�. ���� �������� ��� ���)

            if (!(DatabaseCommandTimeout == DatabaseCommandTimeoutDefaultValue
                    && job.DatabaseCommandTimeout == DatabaseCommandTimeoutDefaultValue))
                DatabaseCommandTimeout = job.DatabaseCommandTimeout;
        }

        #endregion
    }
}