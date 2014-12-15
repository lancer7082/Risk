using System.Collections.Generic;
using System.Linq;
using NLog;

namespace Risk.Jobs
{
    /// <summary>
    /// Менеджер джобов
    /// </summary>
    /// <remarks>
    /// Запускает джобы и добавляет их во внутреннюю коллекцию, удаляет их оттуда и останавливает джобы
    /// </remarks>
    public sealed class JobManager
    {
        /// <summary>
        /// Коллекция джобов
        /// </summary>
        private readonly List<JobBase> _jobs = new List<JobBase>();

        /// <summary>
        /// Лог
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Добавляет и запускает джоб. Джоб не должен быть ранее добавлен.
        /// </summary>
        /// <param name="job">Джоб</param>
        public void AddAndStartJob(JobBase job)
        {
            if (!AddJob(job))
                return;
            job.Start();
        }

        /// <summary>
        /// Добавить джоб
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public bool AddJob(JobBase job)
        {
            // проверка что джоба еще нет в списке
            if (job == null || GetJobByName(job.Name) != null)
                return false;

            _jobs.Add(job);
            Log.Info("Job {0} was added", job.Name);
            return true;
        }

        /// <summary>
        /// Останавливает все джобы и удаляет их из коллекции
        /// </summary>
        public void StopAndRemoveAllJobs()
        {
            for (var i = _jobs.Count - 1; i >= 0; i--)
            {
                StopAndRemoveJob(_jobs[i].Name);
            }
        }

        /// <summary>
        /// Останавливает джоб и удаляет его из коллекции
        /// </summary>
        /// <param name="jobName"></param>
        public void StopAndRemoveJob(string jobName)
        {
            var job = GetJobByName(jobName);
            if (job == null)
                return;
            job.Stop();
            _jobs.Remove(job);
            Log.Info("Job {0} has been removed", job.Name);
        }

        /// <summary>
        /// Перезапускает джоб
        /// </summary>
        /// <param name="jobName"></param>
        public void RestartJob(string jobName)
        {
            var job = GetJobByName(jobName);
            if (job == null)
            {
                Log.Warn("Job {0} not found", jobName);
                return;
            }
            job.Restart();
        }

        /// <summary>
        /// Запускает все джобы
        /// </summary>
        public void StartAllJobs()
        {
            foreach (var job in _jobs)
            {
                if (job.AutoStart)
                    job.Start();
            }
        }

        /// <summary>
        /// Возвращает джоб по его имени
        /// </summary>
        /// <param name="jobName">Имя джоба</param>
        /// <returns></returns>
        public JobBase GetJobByName(string jobName)
        {
            var job = _jobs.SingleOrDefault(s => s.Name == jobName);
            return job;
        }

        /// <summary>
        /// Возвращает джобы по их типу
        /// </summary>
        /// <returns></returns>
        public List<T> GetJobsByType<T>() where T : JobBase
        {
            var jobs = _jobs.OfType<T>().ToList();
            return jobs;
        }

        /// <summary>
        /// Возвращает список всех имен джобов
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllJobsNames()
        {
            return _jobs.Select(s => s.Name).ToList();
        }

        /// <summary>
        /// Идексатор
        /// </summary>
        /// <param name="jobName">Имя джоба</param>
        /// <returns></returns>
        public JobBase this[string jobName]
        {
            get { return GetJobByName(jobName); }
        }
    }
}
