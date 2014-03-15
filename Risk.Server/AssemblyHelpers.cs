using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public static class AssemblyHelpers
    {
        public static string GetTitle(this Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (attributes.Length > 0)
                return ((AssemblyProductAttribute)attributes[0]).Product;
            else
                return Path.GetFileNameWithoutExtension(assembly.CodeBase);
        }

        public static string GetVersion(this Assembly assembly)
        {
            var assemblyName = assembly.GetName();
            DateTime buildDate = new DateTime(2000, 1, 1);
            buildDate = buildDate.AddDays(assemblyName.Version.Build);
            buildDate = buildDate.AddSeconds(assemblyName.Version.Revision * 2);
            return String.Format("{0} ({1}) {2}", assemblyName.Version.ToString(), buildDate, (Environment.Is64BitProcess ? "(x64)" : ""));
        }

        public static string GetCopyright(this Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            if (attributes.Length == 0)
            {
                return "";
            }
            return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
            try
            {
                // return assembly.GetExportedTypes();
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= CurrentDomain_ReflectionOnlyAssemblyResolve;
            }
        }

        static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = Assembly.ReflectionOnlyLoad(args.Name);

            //AssemblyName assemblyName = new AssemblyName(args.Name);
            //var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            return assembly;
        }

        public static Assembly TryReflectionOnlyLoadFrom(string assemblyFile)
        {
            try
            {
                // TODO: ??? var assemblyName = System.Reflection.AssemblyName.GetAssemblyName(asm);
                var assembly = Assembly.ReflectionOnlyLoadFrom(assemblyFile);

                return assembly;
            }
            catch
            {
                return null;
            }
        }
    }
}
