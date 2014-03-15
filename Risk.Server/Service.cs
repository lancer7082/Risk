using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Risk
{
    partial class Service : ServiceBase
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private Server server;

        public Service()
        {
            InitializeComponent();
            AutoLog = false;
            server = new Server();
        }

        protected override void OnStart(string[] args)
        {
            log.Info("Start Service");
            server.Configure();
            server.Start();
        }

        protected override void OnStop()
        {
            log.Info("Stop Service");
            server.Stop();
        }
    }
}
