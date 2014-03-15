using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public class ServerServiceInstaller
    {
        public string GetServiceName(String ServiceName, String InstanceName)
        {
            return String.Format("{0}${1}", ServiceName, InstanceName);
        }

        public void Install(String ServiceName, String InstanceName, String DisplayName, String Description, System.ServiceProcess.ServiceAccount Account, System.ServiceProcess.ServiceStartMode StartMode)
        {
            // http://www.theblacksparrow.com/
            System.ServiceProcess.ServiceProcessInstaller ProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            ProcessInstaller.Account = Account;

            System.ServiceProcess.ServiceInstaller SINST = new System.ServiceProcess.ServiceInstaller();

            System.Configuration.Install.InstallContext Context = new System.Configuration.Install.InstallContext("", null);
            string processPath = Process.GetCurrentProcess().MainModule.FileName;
            if (processPath != null && processPath.Length > 0)
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(processPath);

                String path = String.Format("/assemblypath={0}", fi.FullName);
                String[] cmdline = { path };
                Context = new System.Configuration.Install.InstallContext("install.log", cmdline);
            }

            SINST.Context = Context;
            SINST.ServiceName = GetServiceName(ServiceName, InstanceName);
            SINST.DisplayName = String.Format("{0} ({1})", DisplayName, InstanceName);
            SINST.Description = String.Format("{0}", Description);
            SINST.StartType = StartMode;
            SINST.Parent = ProcessInstaller;
            SINST.ServicesDependedOn = new String[] { };

            System.Collections.Specialized.ListDictionary state = new System.Collections.Specialized.ListDictionary();
            SINST.Install(state);

            // TODO: ???
            //using (RegistryKey oKey = Registry.LocalMachine.OpenSubKey(String.Format(@"SYSTEM\CurrentControlSet\Services\{0}_{1}", ServiceName, InstanceName), true))
            //{
            //    try
            //    {
            //        Object sValue = oKey.GetValue("ImagePath");
            //        oKey.SetValue("ImagePath", sValue);
            //    }
            //    catch (Exception Ex)
            //    {
            //        Console.WriteLine(Ex.Message);
            //    }
            //}
        }
        public void Uninstall(String ServiceName, String InstanceName)
        {
            System.ServiceProcess.ServiceInstaller SINST = new System.ServiceProcess.ServiceInstaller();
            System.Configuration.Install.InstallContext Context = new System.Configuration.Install.InstallContext("uninstall.log", null);
            SINST.Context = Context;
            SINST.ServiceName = GetServiceName(ServiceName, InstanceName);
            SINST.Uninstall(null);
        }
    }
}
