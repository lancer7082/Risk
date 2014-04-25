using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Risk
{
    /// <summary>
    /// Расширения приложения
    /// </summary>
    public class AddIns
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private List<AddInInfo> _addIns = new List<AddInInfo>();
        private IServer server = new ServerProxy();

        public AddInProxy this[string addInTypeName]
        {
            get
            {
                var proxy = _addIns.FirstOrDefault(x => x.AddInTypeName == addInTypeName);
                if (proxy == null)
                    throw new Exception(String.Format("Unknown provider '{0}'", addInTypeName));
                return proxy.Proxy;
            }
        }

        internal AddIns()
        {
        }

        public void Register(string addInTypeName, int resetTime = 0)
        {
            var addInInfo = new AddInInfo(addInTypeName) { ResetTime = resetTime };
            _addIns.Add(addInInfo);
        }

        private void Start(AddInInfo addInInfo)
        {
            try
            {
                AppDomainSetup domainSetup = new AppDomainSetup()
                {
                    ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                    ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
                    LoaderOptimization = LoaderOptimization.MultiDomainHost,
                };

                var domain = AppDomain.CreateDomain("AddInDomain_" + Guid.NewGuid().ToString(), null, domainSetup);
                var addInProxy = (AddInProxy)domain.CreateInstanceAndUnwrap(typeof(AddInProxy).Assembly.FullName, typeof(AddInProxy).FullName);
                addInInfo.Proxy = addInProxy;
                addInInfo.Domain = domain;
                addInProxy.Start(server, addInInfo.AddInTypeName);
                addInInfo.Started = true;
                if (addInInfo.ResetTime > 0)
                {
                    addInInfo.TimerReset = new Timer(c =>
                    {
                        Stop(addInInfo);
                        Start(addInInfo);
                    }, null, addInInfo.ResetTime * 1000, 0);
                }
                log.Info(String.Format("AddIn Load '{0}'", addInInfo.Proxy));
            }
            catch (Exception ex)
            {
                log.Info(String.Format("AddIn Error Load '{0}': {1}", addInInfo.Proxy, ex.Message));
            }
        }

        private void Stop(AddInInfo addInInfo)
        {
            var addInText = addInInfo.Proxy.ToString();
            try
            {
                addInInfo.Proxy.Stop();
                addInInfo.Started = false;
                AppDomain.Unload(addInInfo.Domain);
                log.Info(String.Format("AddIn Unload '{0}'", addInText));
            }
            catch (Exception ex)
            {
                log.Info(String.Format("AddIn Error Unload '{0}': {1}", addInText, ex.Message));
            }
            finally
            {
                addInInfo.Proxy = null;
                addInInfo.Domain = null;
            }
        }

        internal void Start()
        {
            foreach (var addInInfo in _addIns)
            {
                Start(addInInfo);
            }
        }

        internal void Stop()
        {
            foreach (var addInInfo in _addIns)
            {
                if (addInInfo.Started)
                    Stop(addInInfo);
            }
        }

        public void Start(string addInTypeName)
        {
            var addInInfo = _addIns.First(a => a.AddInTypeName == addInTypeName);
            Start(addInInfo);
        }

        public void Stop(string addInTypeName)
        {
            var addInInfo = _addIns.First(a => a.AddInTypeName == addInTypeName);
            Stop(addInInfo);
        }

        private class AddInInfo
        {
            public bool Started { get; set; }
            public string AddInTypeName { get; set; }
            public AddInProxy Proxy { get; set; }
            public AppDomain Domain { get; set; }
            public int ResetTime { get; set; }
            public Timer TimerReset { get; set; }

            public AddInInfo(string addInTypeName)
            {
                this.AddInTypeName = addInTypeName;
            }
        }

        public class AddInProxy : MarshalByRefObject
        {
            private static readonly Logger log = LogManager.GetCurrentClassLogger();
            string addInTypeName;
            private IAddIn instance;

            public override object InitializeLifetimeService()
            {
                // Делаем объект "вечным"
                return null;
            }

            public void Start(IServer server, string addInTypeName)
            {
                Program.AddLogConsole();
                this.addInTypeName = addInTypeName;
                var addInType = Type.GetType(addInTypeName, false);
                if (addInType == null)
                    throw new Exception(String.Format("Not found AddIn type '{0}'", addInTypeName));
                instance = (IAddIn)Activator.CreateInstance(addInType);
                instance.Start(server);
            }

            public void Stop()
            {
                if (instance != null)
                {
                    instance.Stop();
                    instance = null;
                }
            }

            public void Execute(Command command)
            {
                instance.Execute(command);
            }

            public override string ToString()
            {
                return instance == null ? addInTypeName : String.Format("{0} {1}", instance.Name(), instance.Version());
            }
        }

        private class ServerProxy : MarshalByRefObject, IServer
        {
            public T Execute<T>(Command command)
            {
                return (T)Execute(command);
            }

            public object Execute(Command command)
            {
                return Server.Current.Execute(command);
            }

            public void ExecuteAsync(Command command)
            {
                Server.Current.ExecuteAsync(command);
            }
        }
    }
}