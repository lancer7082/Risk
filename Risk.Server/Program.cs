using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Risk
{
    class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static bool ArgExists(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (String.Equals(args[i], name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string ArgValue(string[] args, string name, string defaultValue = null)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (String.Equals(args[i], name, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (i + 1 < args.Length && !String.IsNullOrWhiteSpace(args[i + 1]) && args[i + 1].TrimStart()[0] != '-')
                        return args[i + 1];
                    else
                        return defaultValue;
                }
            }
            return defaultValue;
        }

        public static void AddLogConsole()
        {
            if (LogManager.Configuration == null)
                LogManager.Configuration = new LoggingConfiguration();
            var config = LogManager.Configuration;
            ColoredConsoleTarget consoleTarget = config.AllTargets.FirstOrDefault(t => t.Name == "console") as ColoredConsoleTarget;
            if (consoleTarget == null)
            {
                consoleTarget = new ColoredConsoleTarget();
                consoleTarget.Layout = @"${date:format=HH\:mm\:ss}| ${message}";
                config.AddTarget("console_" + Guid.NewGuid().ToString(), consoleTarget);
                LoggingRule consoleRule = new LoggingRule("*", LogLevel.Info, consoleTarget);
                config.LoggingRules.Add(consoleRule);
                LogManager.Configuration = config;
            }
        }

        private static void StartNewStaThread() 
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormDebug()); 
        }

        const string ServiceName = "FinamRiskServer";
        const string ServiceDisplayName = "Finam Risk Server";
        const string ServiceDescription = "Finam Risk Management Server";
        const string ServiceDefaultInstanceName = "RISK";

        private static bool consoleMode = false;

        static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) => { log.FatalException(String.Format("Unhandled exception: {0}", ((Exception)e.ExceptionObject).Message), e.ExceptionObject as Exception); LogManager.Flush(); };
                Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string instanceName = ArgValue(args, "-s", ServiceDefaultInstanceName);

                // Install
                if (ArgExists(args, "-i"))
                {
                    ServerServiceInstaller installer = new ServerServiceInstaller();
                    try
                    {
                        installer.Install(ServiceName, instanceName, ServiceDisplayName, ServiceDescription, System.ServiceProcess.ServiceAccount.LocalSystem, System.ServiceProcess.ServiceStartMode.Automatic);
                        ServiceController controller = new System.ServiceProcess.ServiceController(installer.GetServiceName(ServiceName, instanceName), ".");
                        if (controller.Status != System.ServiceProcess.ServiceControllerStatus.Running)
                            controller.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                        return;
                    }
                }
                else if (ArgExists(args, "-u"))
                {
                    ServerServiceInstaller Inst = new ServerServiceInstaller();
                    try
                    {
                        ServiceController controller = new System.ServiceProcess.ServiceController(Inst.GetServiceName(ServiceName, instanceName), ".");
                        Inst.Uninstall(ServiceName, instanceName);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                        return;
                    }
                }
                else if (ArgExists(args, "-c") || ArgExists(args, "-console"))
                {
                    consoleMode = true;

                    AddLogConsole();

                    log.Info("Start console {0}", ServiceDisplayName);
                    Server server = new Server();
                    if (ArgExists(args, "-safe"))
                        server.SafeMode = true;
                    server.Configure();
                    server.Start();

                    Console.WriteLine("To stop, press any key . . .");
                    Console.ReadKey();

                    server.Stop();
                    log.Info("Stop console {0}", ServiceDisplayName);

                    Console.WriteLine("To exit, press any key . . .");
                    Console.ReadKey();
                }
                else if (ArgExists(args, "-d") || ArgExists(args, "-debug"))
                {
                    consoleMode = true;
                    AddLogConsole();

                    Thread threadApplication = new Thread(new ThreadStart(StartNewStaThread));
                    threadApplication.SetApartmentState(ApartmentState.STA);
                    threadApplication.Start();
                }
                else
                {
                    Service service = new Service();
                    service.ServiceName = instanceName;
                    ServiceBase.Run(service);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                LogManager.Flush();

                if (consoleMode)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine("To exit, press any key . . .");
                    Console.ReadKey();
                }
            }
        }
    }
}
