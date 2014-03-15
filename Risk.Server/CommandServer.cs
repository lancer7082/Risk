using System;
using System.Threading;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Базовый класс команды сервера
    /// </summary>
    internal class CommandServer : Command
    {
        public Connection Connection { get; set; }
        public CancellationToken CancellationToken { get; private set; }
        public TaskCompletionSource<object> TaskCompletionSource { get; private set; }

        internal CommandServer()
        {
            CancellationToken = new CancellationToken();
        }

        internal CommandServer(Connection connection, Command command)
            : this()
        {
            this.Connection = connection;
            this.Type = command.Type;
            this.Text = command.Text;
            this.CorrelationId = command.CorrelationId;
            this.Data = command.Data;
        }

        public void ExecuteAsync()
        {
            Server.ProcessCommand(this);
        }

        public object Execute()
        {
            TaskCompletionSource = new TaskCompletionSource<object>();
            Server.ProcessCommand(this);
            try
            {
                TaskCompletionSource.Task.Wait(CancellationToken);
            }
            catch (AggregateException ex)
            {
                throw ex.InnerExceptions[0];
            }
            if (TaskCompletionSource.Task.Exception != null)
                throw TaskCompletionSource.Task.Exception;
            else
                return TaskCompletionSource.Task.Result;
        }

        public void SetResult(object value)
        {
            TaskCompletionSource.SetResult(value);
        }
    }
}