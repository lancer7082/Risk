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
        /// <summary>
        /// Тип объекта
        /// </summary>
        public string ObjectType
        {
            get { return (string)Parameters["ObjectType"]; }
            set { Parameters["ObjectType"] = value; }
        }

        protected internal override void InternalExecute()
        {
            var items = new List<HierarchyObject>();

            // Tables
            if (ObjectType == null || ObjectType == "Tables")
            {
                var tables = new HierarchyObject { Name = "Tables" };
                items.Add(tables);

                var tablesItems = (from t in Server.Current.DataObjects
                                   where t is ITable
                                   select new HierarchyObject { Parent = tables, Name = t.Name, Data = new ServerObjectInfo { Name = t.Name, ObjectType = "Table" } }).ToArray();
            }

            // Objects
            if (ObjectType == null || ObjectType == "Objects")
            {
                var objects = new HierarchyObject { Name = "Objects" };
                items.Add(objects);

                var objectsItems = (from o in Server.Current.DataObjects
                                    where !(o is ITable)
                                    select new HierarchyObject { Parent = objects, Name = o.Name, Data = new ServerObjectInfo { Name = o.Name, ObjectType = "Object" } }).ToArray();
            }

            // Commands
            if (ObjectType == null || ObjectType == "Commands")
            {
                var commands = new HierarchyObject { Name = "Commands" };
                items.Add(commands);

                var commandsItems = (from c in Server.Current.Commands
                                     select new HierarchyObject { Parent = commands, Name = c.Key, Data = new ServerObjectInfo { Name = c.Key, ObjectType = "Command" } }).ToArray();
            }

            // Jobs
            if (ObjectType == null || ObjectType == "Jobs")
            {
                var jobs = new HierarchyObject { Name = "Jobs" };
                items.Add(jobs);
            }

            if (ObjectType == null)
                SetResult(items.ToArray());
            else if (items.Count > 0)
                SetResult(items[0].Items.ToArray());
        }
    }
}
