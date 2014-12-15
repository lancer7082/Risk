using System;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Комманда обрабатываемая сервером
    /// </summary>
    public abstract class CommandServer
    {
        private ParameterCollection _params;

        public Connection Connection { get; set; }
        public string CorrelationId { get; set; }
        public IDataObject Object { get; set; }
        public FieldInfo[] FieldsInfo { get; set; }
        public object Data { get; set; }

        public ParameterCollection Parameters
        {
            get
            {
                if (_params == null)
                    _params = new ParameterCollection();
                return _params;
            }

            set
            {
                if (_params != null)
                    _params.Clear();
                else
                    _params = new ParameterCollection();
                foreach (var param in value)
                {
                    _params.Add(param);
                }
                PrepareParameters();
            }
        }

        public CancellationToken CancellationToken { get; private set; }
        public TaskCompletionSource<object> TaskCompletionSource { get; private set; }

        protected virtual void PrepareParameters()
        {
        }

        protected internal abstract void InternalExecute();

        internal void SetResult(object value)
        {
            Data = value;
            if (TaskCompletionSource != null)
                TaskCompletionSource.SetResult(value);
        }

        public void ExecuteAsync()
        {
            Server.ProcessCommand(this);
        }

        public object Execute()
        {
            TaskCompletionSource = new TaskCompletionSource<object>();
            try
            {
                Server.ProcessCommand(this);
            }
            catch
            {
                throw;
            }
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

        // !!! Only for unit tests !!!
        // TODO: !!! Remove to unit tests helper
        public object ExecuteOnlyForUnitTest()
        {
            InternalExecute();
            return Data;
        }
    }
}