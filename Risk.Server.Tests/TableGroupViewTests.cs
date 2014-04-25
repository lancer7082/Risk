using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Risk.Tests
{
    public class TableData
    {
        public int Key { get; set; }
        public int Key1 { get; set; }
        public string Code { get; set; }
        public decimal Value { get; set; }
        public decimal Value1 { get; set; }
        public int ValueUpdated { get; set; }
        public int ValueCalculated { get; set; }
    }
    
    [TestClass]
    public class TableGroupViewTests
    {
        static TableGroupViewTests()
        {
            Server.WithoutProcessQueue = true;
        }

        [Table("TableTest", KeyFields = "Key,Key1")]
        public class TableTest : Table<TableData>
        {
            private Server _server;

            public object Execute(Command command)
            {
                if (_server == null)
                {
                    _server = new Server();
                    _server.Start();
                    _server.Register(table);
                    _server.Register(view);
                    
                }

                command.Parameters["ObjectName"] = "TableTest";
                return _server.Execute(command);
            }

            public override void TriggerAfter(TriggerCollection<TableData> items)
            {
                foreach (var item in items)
                    item.Updated.ValueCalculated = item.Updated.ValueUpdated * 100;

                var groupItems = from pair in items
                                 group pair by new { pair.Updated.Key } into g
                                 select new TableData
                                 {
                                    Key = g.Key.Key,
                                    Code = g.First().Updated.Code,
                                    Value = g.CumulativeSum(x => x.Value),
                                    Value1 = g.CumulativeSum(x => x.Value1),
                                    ValueUpdated = g.CumulativeSum(x => x.ValueUpdated),
                                    ValueCalculated = g.CumulativeSum(x => x.ValueCalculated),
                                };

                var result = new CommandMerge
                    {
                        Object = view,
                        Data = groupItems,
                        Fields = "@Value,@Value1,@ValueUpdated,@ValueCalculated",
                        KeyFields = "Key",
                    }.ExecuteOnlyForUnitTest();
            }
        }

        [Table("ViewTest", KeyFields = "Key")]
        public class ViewTest : Table<TableData>
        {
        }

        private static TableTest table = new TableTest();
        private static ViewTest view = new ViewTest();

        [TestMethod]
        public void TableGroupViewInsertTest()
        {
            table.Clear();
            view.Clear();
            table.Execute(Command.Insert(null,
                new TableData[] 
                {
                    new TableData { Key = 1, Key1 = 1, Code = "1", Value = 1, Value1 = 1 },
                    new TableData { Key = 1, Key1 = 2, Code = "1", Value = 1, Value1 = 2 },
                    new TableData { Key = 1, Key1 = 3, Code = "1", Value = 1, Value1 = 3 },
                    new TableData { Key = 2, Key1 = 1, Code = "2", Value = 2, Value1 = 1 },
                    new TableData { Key = 2, Key1 = 2, Code = "2", Value = 2, Value1 = 2 },
                }
            ));

            var result = view.Select(x => x).ToArray();
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(x => x.Key == 1 && x.Code == "1" && x.Value == 3 && x.Value1 == 6));
            Assert.AreEqual(1, result.Count(x => x.Key == 2 && x.Code == "2" && x.Value == 4 && x.Value1 == 3));
        }

        [TestMethod]
        public void TableGroupViewUpdateTest()
        {
            TableGroupViewInsertTest();

            table.Execute(Command.Update(null,
                new TableData[] 
                {
                    new TableData { Key = 1, Key1 = 2, Code = "1", Value = 0, Value1 = 0 },
                    new TableData { Key = 1, Key1 = 3, Code = "1", Value = 1, Value1 = 1 },
                },
                "@Value,Value1"));

            var result = view.Select(x => x).ToArray();
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(x => x.Key == 1 && x.Code == "1" && x.Value == 4 && x.Value1 == 2));
            Assert.AreEqual(1, result.Count(x => x.Key == 2 && x.Code == "2" && x.Value == 4 && x.Value1 == 3));
        }

        [TestMethod]
        public void TableGroupViewUpdateFieldsTest()
        {
            TableGroupViewInsertTest();

            table.Execute(Command.Update(null,
                new TableData[] 
                {
                    new TableData { Key = 1, Key1 = 2, Code = "1", Value = 0, Value1 = 0 },
                    new TableData { Key = 1, Key1 = 3, Code = "1", Value = 0, Value1 = 1 },
                },
                "Code,Value1"));

            var result = view.Select(x => x).ToArray();
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(x => x.Key == 1 && x.Code == "1" && x.Value == 3 && x.Value1 == 2));
            Assert.AreEqual(1, result.Count(x => x.Key == 2 && x.Code == "2" && x.Value == 4 && x.Value1 == 3));
        }

        [TestMethod]
        public void TableGroupViewMergeTest()
        {
            TableGroupViewInsertTest();

            table.Execute(Command.Merge(null, 
                new TableData[]
                {
                    new TableData { Key = 1, Key1 = 2, Code = "1", Value = 0, Value1 = 0 },
                    new TableData { Key = 1, Key1 = 3, Code = "1", Value = 1, Value1 = 1 },
                    new TableData { Key = 1, Key1 = 4, Code = "1", Value = 1, Value1 = 1 },
                },
                "@Value,Value1", "Key"));

            var result = view.Select(x => x).ToArray();
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(x => x.Key == 1 && x.Code == "1" && x.Value == 4 && x.Value1 == 2));
            Assert.AreEqual(1, result.Count(x => x.Key == 2 && x.Code == "2" && x.Value == 4 && x.Value1 == 3));
        }

        /// <summary>
        /// FIX: При изменении Updated не работает CumulativeUpdate
        /// </summary>
        [TestMethod]
        public void TableGroupViewCumulativeUpdateTest()
        {
            TableGroupViewInsertTest();

            table.Execute(Command.Merge(null, 
                new TableData[]
                {
                    new TableData { Key = 1, Key1 = 2, ValueUpdated = 1 },
                    new TableData { Key = 1, Key1 = 3, ValueUpdated = 2 },
                    new TableData { Key = 1, Key1 = 4, ValueUpdated = 3 },
                },
                "@ValueUpdated", "Key"));

            var result = view.Select(x => x).ToArray();
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(x => x.Key == 1 && x.Code == "1" && x.ValueUpdated == 6 && x.ValueCalculated == 600)); // 600
            Assert.AreEqual(1, result.Count(x => x.Key == 2 && x.Code == "2" && x.ValueUpdated == 0 && x.ValueCalculated == 0)); // 0
        }
    }
}