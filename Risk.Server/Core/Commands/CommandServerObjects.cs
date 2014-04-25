using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    /// <summary>
    /// Список объектов сервера
    /// </summary>
    [Command("ServerObjects")]
    public class CommandServerObjects : CommandServer
    {
        protected internal override void InternalExecute()
        {
            var items = new List<HierarchyObject>();

            // Tables
            var tables = new HierarchyObject { Name = "Tables" };
            items.Add(tables);

            var tablesItems = (from t in Server.Current.DataObjects
                               where t is ITable
                               select new HierarchyObject { Parent = tables, Name = t.Name, Data = new ServerObjectInfo { Name = t.Name, ObjectType = "Table" } }).ToArray();

            // Objects
            var objects = new HierarchyObject { Name = "Objects" };
            items.Add(objects);

            var objectsItems = (from o in Server.Current.DataObjects
                                where !(o is ITable)
                                select new HierarchyObject { Parent = objects, Name = o.Name, Data = new ServerObjectInfo { Name = o.Name, ObjectType = "Object" } }).ToArray();

            // Commands
            var commands = new HierarchyObject { Name = "Commands" };
            items.Add(commands);

            var commandsItems = (from c in Server.Current.Commands
                                 select new HierarchyObject { Parent = commands, Name = c.Key, Data = new ServerObjectInfo { Name = c.Key, ObjectType = "Command" } }).ToArray();

            // Jobs
            var jobs = new HierarchyObject { Name = "Jobs" };
            items.Add(jobs);

            SetResult(items.ToArray());
        }
    }
}
