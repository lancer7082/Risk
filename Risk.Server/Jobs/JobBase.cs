using System;
using System.Threading;
using NLog;

namespace Risk.Jobs
{
    /// <summary>
    /// Базовый класс всех джобов
    /// </summary>
    /// <remarks>
    /// Содержит таймер, переодически выполняющий рабочий метод
    /// </remarks>
    public abstract class JobBase
    {
        /// <summary>
        /// Объект таймера
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// Джоб запущен и работает
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// Период выполнения таймера
        /// </summary>
        public int Period { get; set; }

        /// <summary>
        /// Enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// AutoStart
        /// </summary>
        public bool AutoStart { get; set; }

        /// <summary>
        /// Имя джоба. Сопадает с типом по умолчанию
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Лог
        /// </summary>
        protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Конструктор
        /// </summary>
        protected JobBase()
        {
            AutoStart = true;
            _timer = new Timer(DoWork);
        }

        /// <summary>
        /// Тело выполняемого по таймеру метода
        /// </summary>
        /// <param name="data"></param>
        protected abstract void DoWork(object data);

        /// <summary>
        /// Перезапуск джоба
        /// </summary>
        public void Restart()
        {
            Start();
        }

        /// <summary>
        /// Запуск джоба
        /// </summary>
        public void Start()
        {
            Start(Period);
        }

        /// <summary>
        /// Запуск джоба
        /// </summary>
        /// <param name="periodMilliSeconds">Период выполнения в миллисекундах</param>
        private void Start(int periodMilliSeconds)
        {
            Restart(new TimeSpan(), new TimeSpan(0, 0, 0, 0, periodMilliSeconds));
        }

        /// <summary>
        /// Остановка джоба
        /// </summary>
        public void Stop()
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _isRunning = false;
            Log.Info("Job {0} has been stopped", Name);
        }

        /// <summary>
        /// Перезапуск джоба. 
        /// </summary>
        /// <param name="dueTime">Время до начала старта</param>
        /// <param name="period">Период выполнения</param>
        /// <remarks> Основной метод. Все перегруженные методы в конечном итоге вызывают этот</remarks>
        private void Restart(TimeSpan dueTime, TimeSpan period)
        {
            // если джоб еще не запущен
            if (!_isRunning)
            {
                if (Enabled)
                {
                    // запускаем, устанавливаем флаг запуска
                    _timer.Change(dueTime, period);
                    _isRunning = true;
                    Log.Info("Job {0} has been started", Name);
                }
                else
                    Log.Warn("Can't started job {0} because it is disabled", Name);
            }
            // джоб уже запущен
            else
            {
                if (Enabled)
                {
                    Log.Info("Restarting job {0}...", Name);
                    // меняем параметры
                    _timer.Change(dueTime, period);
                }
                else
                {
                    // останалвиваем
                    Stop();
                    Log.Warn("Job {0} {1}", Name, "has been disabled");
                }
            }
        }

        /// <summary>
        /// Пишет информационное сообщение в лог
        /// </summary>
        /// <param name="message"></param>
        protected void WriteInfoLog(string message)
        {
            Log.Info("Job {0} info: {1}", Name, message);
        }

        /// <summary>
        /// Пишет сообщение об ошибке в лог
        /// </summary>
        /// <param name="message"></param>
        protected void WriteErrorLog(string message)
        {
            Log.Error("Job {0} error: {1}", Name, message);
        }

        /// <summary>
        /// Возвращает пустую строку, если она null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected string GetEmptyStringIfNull(string value)
        {
            return value ?? string.Empty;
        }
    }
}
