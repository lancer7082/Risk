using System.Configuration;
using System.Linq;
using Risk.Jobs;

namespace Risk.Configuration.JobConfiguration
{
    /// <summary>
    /// ���������� ������ ������ - �������� ��������� �� ����� ������
    /// </summary>
    public class JobTypesConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// �����, ���������� � ����� ������
        /// </summary>
        [ConfigurationProperty("databaseJobs")]
        [ConfigurationCollection(typeof(DatabaseJobConfigurationElement), AddItemName = "job")]
        public BaseJobConfigurationElementCollection<DatabaseJobConfigurationElement> DatabaseJobs
        {
            get
            {
                return this["databaseJobs"] as BaseJobConfigurationElementCollection<DatabaseJobConfigurationElement>;
            }
            set { this["databaseJobs"] = value; }
        }

        /// <summary>
        /// �����, ���������� ��������
        /// </summary>
        [ConfigurationProperty("delegateJobs")]
        [ConfigurationCollection(typeof(DelegateJobConfigurationElement), AddItemName = "job")]
        public BaseJobConfigurationElementCollection<DelegateJobConfigurationElement> DelegateJobs
        {
            get
            {
                return this["delegateJobs"] as BaseJobConfigurationElementCollection<DelegateJobConfigurationElement>;
            }
        }

        /// <summary>
        /// ��������� ������ � �������
        /// </summary>
        /// <param name="server"></param>
        public void ApplyConfig(Server server)
        {
            if (server == null)
                return;

            if (DatabaseJobs != null)
                DatabaseJobs.ApplyConfig(server);
            if (DelegateJobs != null)
                DelegateJobs.ApplyConfig(server);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        public void UpdateConfig(Server server)
        {
            if (server == null || server.JobManager == null)
                return;

            // ���� ������, �� �������
            if (DatabaseJobs == null)
                DatabaseJobs = new BaseJobConfigurationElementCollection<DatabaseJobConfigurationElement>();
            
            // ���� � ������������� ����� �����, ������� ��� ��� � �������
            var databaseJobs = server.JobManager.GetJobsByType<DatabaseJob>();
            
            var newDatabaseJobs = databaseJobs.Where(s => 
                DatabaseJobs.Cast<DatabaseJobConfigurationElement>().All(job => job.Name != s.Name)).ToList();
           
            if (newDatabaseJobs.Any())
            {
                newDatabaseJobs.ForEach(j => DatabaseJobs.Add(new DatabaseJobConfigurationElement()
                {
                    Name = j.Name
                }));
            }

            // ��������� ������
            DatabaseJobs.UpdateConfig(server);
        }
    }
}