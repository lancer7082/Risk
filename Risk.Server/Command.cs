using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risk
{
    [Serializable]
    [KnownType(typeof(CommandNotify))] // TODO: ??? http://msdn.microsoft.com/en-us/library/ms730167(v=vs.110).aspx
    public class Command
    {
        public Connection Connection { get; internal set; }

        [NonSerialized]
        internal TaskCompletionSource<object> taskCompletionSource;

        [NonSerialized]
        internal CancellationToken cancellationToken = new CancellationToken();

        [NonSerialized]
        internal TaskScheduler taskScheduler;

        public string Text { get; set; }

        public object Result
        {
            set
            {
                if (taskCompletionSource != null)
                    taskCompletionSource.SetResult(value);
            }
        }

        public Command()
        {
            taskCompletionSource = new TaskCompletionSource<object>();
            if (SynchronizationContext.Current != null)
                taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public void ExecuteAsync()
        {
            Server.ProcessCommand(this);
        }

        public T Execute<T>()
        {
            return (T)Execute();
        }

        public virtual object Execute()
        {
            taskCompletionSource = new TaskCompletionSource<object>();
            Server.ProcessCommand(this);
            if (taskCompletionSource != null)
            {
                try
                {
                    taskCompletionSource.Task.Wait(cancellationToken);
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerExceptions[0];
                }
                if (taskCompletionSource.Task.Exception != null)
                    throw taskCompletionSource.Task.Exception;
                else
                    return taskCompletionSource.Task.Result;
            }
            else
                return null;
        }
    }
}