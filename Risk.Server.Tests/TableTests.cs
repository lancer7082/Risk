using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Risk.Tests
{
    [TestClass]
    public class TableTests
    {
        static TableTests()
        {
            Server.WithoutProcessQueue = true;
        }

        public class TableData : ICloneable
        {
            public int Key { get; set; }
            public string Value { get; set; }
            public string Value1 { get; set; }
            public int ValueInt { get; set; }

            public object Clone()
            {
                return new TableData
                {
                    Key = Key,
                    Value = Value,
                    Value1 = Value1,
                    ValueInt = ValueInt
                };
            }
        }

        [Table("TableTest", KeyFields = "Key")]
        public class TableTest : Table<TableData>
        {
            private Server _server;

            public int TriggerInsertCount;
            public int TriggerUpdateCount;
            public int TriggerDeleteCount;

            public TableTest(int count = 0)
            {
                _server = new Server();
                _server.Start();
                _server.Register(this);
                for ( int i = 0; i < count; i++ )
                    base.Add(new TableData { Key = i, Value = "Value_" + i.ToString(), Value1 = "Value2_" + i.ToString() });
            }

            protected override System.Linq.Expressions.Expression<Func<TableData, bool>> Predicate(ParameterCollection parameters)
            {
                var predicate = base.Predicate(parameters);

                var boolParam = object.Equals(parameters["BoolParam"], true);
                if (boolParam)
                    predicate = predicate.And(x => x.Key == 1000);

                return predicate;
            }

            public object Execute(Command command)
            {
                command.Parameters["ObjectName"] = Name;
                return _server.Execute(command);
            }

            public override void TriggerAfter(TriggerCollection<TableData> items)
            {
                var rowAffected = from i in items
                                  group i by i.Action into g
                                  select new { Action = g.Key, Count = g.Count() };
                foreach (var row in rowAffected)
                switch (row.Action)
                {
                    case TriggerAction.Insert: TriggerInsertCount += row.Count; break;
                    case TriggerAction.Update: TriggerUpdateCount += row.Count; break;
                    case TriggerAction.Delete: TriggerDeleteCount += row.Count; break;
                }
            }

            public void TriggerCountClear()
            {
                TriggerInsertCount = 0;
                TriggerUpdateCount = 0;
                TriggerDeleteCount = 0;
            }
        }

        [Table("TableTestManyKeys", KeyFields = "Key,Value")]
        public class TableTestManyKeys : TableTest
        {
        }

        [TestMethod]
        public void TableSelectTest()
        {
            var table = new TableTest(100000);
            var stopWatch = new Stopwatch();
            
            stopWatch.Start();
            var result = table.Select(x => x);
            stopWatch.Stop();
            
            var ts = stopWatch.Elapsed;

            Assert.AreEqual(100000, result.Count());
        }

        [TestMethod]
        public void TableSelectParamTest()
        {
            var table = new TableTest(100000);
            var stopWatch = new Stopwatch();
            
            stopWatch.Start();
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null, null, new ParameterCollection { new Parameter { Name = "[Key]", Value = 1000 } } ) );
            stopWatch.Stop();

            var ts = stopWatch.Elapsed;
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(1000, result.First().Key);
            Assert.AreEqual("Value_1000", result.First().Value);
        }

        [TestMethod]
        public void TableSelectFilterTest()
        {
            var table = new TableTest(100000);
            var stopWatch = new Stopwatch();
            
            stopWatch.Start();
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null, "Key = 1000"));
            stopWatch.Stop();
            
            var ts = stopWatch.Elapsed;
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(1000, result.First().Key);
            Assert.AreEqual("Value_1000", result.First().Value);
        }

        [TestMethod]
        public void TableSelectComplexFilterTest()
        {
            var table = new TableTest(100000);
            var stopWatch = new Stopwatch();
            
            stopWatch.Start();
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null, @"(Key = 1000) OR (Value = ""Value_2000"")" ));
            stopWatch.Stop();
            
            var ts = stopWatch.Elapsed;
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1000, result.First().Key);
            Assert.AreEqual("Value_1000", result.First().Value);
            Assert.AreEqual(2000, result.ElementAt(1).Key);
            Assert.AreEqual("Value_2000", result.ElementAt(1).Value);
        }

        [TestMethod]
        public void TableSelectParamAndFilterTest()
        {
            var table = new TableTest(100000);
            var stopWatch = new Stopwatch();
            
            stopWatch.Start();
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null, @"(Key = 1000) OR (Value = ""Value_2000"")", new ParameterCollection { new Parameter { Name = "[Key]", Value = 1000 } } ));
            stopWatch.Stop();
            
            var ts = stopWatch.Elapsed;
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(1000, result.First().Key);
            Assert.AreEqual("Value_1000", result.First().Value);
        }


        [TestMethod]
        public void TableSelectPredicateTest()
        {
            var table = new TableTest(100000);
            var stopWatch = new Stopwatch();

            stopWatch.Start();
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null, @"(Key = 1000) OR (Value = ""Value_2000"")", new ParameterCollection { new Parameter { Name = "[Key]", Value = 1000 }, new Parameter { Name = "BoolParam", Value = true } }));
            stopWatch.Stop();

            var ts = stopWatch.Elapsed;
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(1000, result.First().Key);
            Assert.AreEqual("Value_1000", result.First().Value);
        }

        [TestMethod]
        public void TableInsertTest()
        {
            int count = 1000;

            var table = new TableTest(0);
            var stopWatch = new Stopwatch();
            var items = new List<TableData>();
            for ( int i = 0; i < count; i++ )
                items.Add(new TableData { Key = i, Value = "Value_" + i.ToString(), Value1 = "Value2_" + i.ToString() });

            stopWatch.Start();
            table.Execute(Command.Insert(null, items));
            stopWatch.Stop();

            var ts = stopWatch.Elapsed;
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null));
            Assert.IsNotNull(result);
            Assert.AreEqual(count, result.Count());
        }

        [TestMethod]
        public void TableUpdateTest()
        {
            int count = 10000;

            var table = new TableTest(count);
            var stopWatch = new Stopwatch();
            var items = new List<TableData>();
            for (int i = 0; i < count; i++)
                items.Add(new TableData { Key = i, Value = "UpdatedValue_" + i.ToString(), Value1 = "UpdatedValue2_" + i.ToString() });

            stopWatch.Start();
            table.Execute(Command.Update(null, items));
            stopWatch.Stop();

            var ts = stopWatch.Elapsed;
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null));
            Assert.IsNotNull(result);
            Assert.AreEqual(count, result.Count());
        }

        [TestMethod]
        public void TableUpdateManyRowTest()
        {
            var table = new TableTestManyKeys();
            table.Execute(Command.Insert(null,
                new TableData[] 
                {
                    new TableData { Key = 1, Value = "1", Value1 = "1_1" },
                    new TableData { Key = 1, Value = "2", Value1 = "1_2" },
                    new TableData { Key = 1, Value = "3", Value1 = "1_3" },
                    new TableData { Key = 2, Value = "1", Value1 = "2_1" },
                    new TableData { Key = 2, Value = "2", Value1 = "2_2" },
                    new TableData { Key = 2, Value = "3", Value1 = "2_3" }
                }
            ));
            
            var items = new List<TableData>();
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "Updated_1_1" });
            table.Execute(Command.Update(null, items));
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null, null, new ParameterCollection { new Parameter { Name = "[Key]", Value = 1 }, new Parameter { Name = "[Value]", Value = "1" } }));
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public void TableManyKeysUpdateTest()
        {
             var table = new TableTestManyKeys();
            table.Execute(Command.Update(null, 
                new TableData[] 
                {
                    new TableData { Key = 1, Value = "1", Value1 = "1_1" },
                    new TableData { Key = 1, Value = "2", Value1 = "1_2" },
                    new TableData { Key = 1, Value = "3", Value1 = "1_3" },
                    new TableData { Key = 2, Value = "1", Value1 = "2_1" },
                    new TableData { Key = 2, Value = "2", Value1 = "2_2" },
                    new TableData { Key = 2, Value = "3", Value1 = "2_3" }
                }));

            var items = new List<TableData>();
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "Updated_1_1" });
            table.Execute(Command.Insert(null, items));
            var result = (IEnumerable<TableData>)table.Execute(Command.Select(null, null, new ParameterCollection { new Parameter { Name = "Key", Value = 1 }, new Parameter { Name = "Value", Value = "1" } }));
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
        }

        [TestMethod]
        public void TableUpdateFieldsFromParamsTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "1_1", ValueInt = 1 });
            items.Add(new TableData { Key = 2, Value = "2", Value1 = "2_1", ValueInt = 2 });
            items.Add(new TableData { Key = 3, Value = "3", Value1 = "3_1", ValueInt = 3 });

            table.Execute(Command.Insert(null, items));
            Assert.AreEqual(3, table.TriggerInsertCount);

            // Update
            table.Execute(new Command
            {
                CommandText = "update",
                Parameters = new ParameterCollection 
                { 
                    new Parameter { Name = "[Key]", Value = 2 },
                    new Parameter { Name = "[Value]", Value = "Updated_2" },
                    new Parameter { Name = "[ValueInt]", Value = 555 },
                }});

            var result = table.Select(x => x).ToArray();
            Assert.AreEqual(3, result.Count());
            Assert.AreEqual(1, table.Count(x => x.Key == 1 && x.Value == "1" && x.Value1 == "1_1" && x.ValueInt == 1));
            Assert.AreEqual(1, table.Count(x => x.Key == 2 && x.Value == "Updated_2" && x.Value1 == "2_1" && x.ValueInt == 555));
            Assert.AreEqual(1, table.Count(x => x.Key == 3 && x.Value == "3" && x.Value1 == "3_1" && x.ValueInt == 3));
        }

        [TestMethod]
        public void TableDeleteTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1" });
            items.Add(new TableData { Key = 2, Value = "2" });
            items.Add(new TableData { Key = 3, Value = "3" });
            items.Add(new TableData { Key = 4, Value = "4" });
            items.Add(new TableData { Key = 5, Value = "5" });

            table.Execute(Command.Insert(null, items));

            Assert.AreEqual(5, table.TriggerInsertCount);

            // Delete
            items.Clear();
            items.Add(new TableData { Key = 2 });
            items.Add(new TableData { Key = 3 });
            table.Execute(Command.Delete(null, items));

            var result = table.Select(x => x).ToArray();
            Assert.AreEqual(3, result.Count());
            Assert.AreEqual(0, table.Count(x => x.Key == 2));
            Assert.AreEqual(0, table.Count(x => x.Key == 3));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TableCheckConstraintTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "2" });
            items.Add(new TableData { Key = 2, Value = "1", Value1 = "2" });
            items.Add(new TableData { Key = 3, Value = "2", Value1 = "1" });
            items.Add(new TableData { Key = 4, Value = "2", Value1 = "2" });
            table.Execute(Command.Insert(null, items));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TableCheckConstraintManyKeysTest()
        {
            var table = new TableTestManyKeys();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 2, Value = "2", Value1 = "2" });
            items.Add(new TableData { Key = 2, Value = "1", Value1 = "2" });
            items.Add(new TableData { Key = 2, Value = "2", Value1 = "1" });
            items.Add(new TableData { Key = 4, Value = "2", Value1 = "2" });
            table.Execute(Command.Insert(null, items));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TableCheckConstraintInsertTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 2, Value = "1", Value1 = "2" });
            items.Add(new TableData { Key = 3, Value = "2", Value1 = "1" });
            items.Add(new TableData { Key = 4, Value = "2", Value1 = "2" });
            table.Execute(Command.Insert(null, items));

            Assert.AreEqual(4, table.TriggerInsertCount);

            // Insert
            table.TriggerCountClear();
            items.Clear();
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 2, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 3, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 4, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 5, Value = "1", Value1 = "1" });

            table.Execute(Command.Insert(null, items));
        }

        [TestMethod]
        public void TableMergeTest()
        {
            var table = new TableTestManyKeys();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 1, Value = "2", Value1 = "2" });
            items.Add(new TableData { Key = 2, Value = "1", Value1 = "1" });
            items.Add(new TableData { Key = 3, Value = "3", Value1 = "3" });
            table.Execute(Command.Insert(null, items));

            // Insert
            items.Clear();
            items.Add(new TableData { Key = 1, Value = "1", Value1 = "new_1" });
            items.Add(new TableData { Key = 1, Value = "3", Value1 = "new_3" });
            items.Add(new TableData { Key = 2, Value = "2", Value1 = "new_2" });
            table.Execute(Command.Merge(null, items, null, "Key"));

            var result = table.Select(x => x).ToArray();
            Assert.AreEqual(4, result.Count());
            Assert.AreEqual(1, table.Count(x => x.Key == 1 && x.Value == "1" && x.Value1 == "new_1"));
            Assert.AreEqual(1, table.Count(x => x.Key == 1 && x.Value == "3" && x.Value1 == "new_3"));
            Assert.AreEqual(1, table.Count(x => x.Key == 2 && x.Value == "2" && x.Value1 == "new_2"));
            Assert.AreEqual(1, table.Count(x => x.Key == 3 && x.Value == "3" && x.Value1 == "3"));
        }

        [TestMethod]
        public void TableMergeFullTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1" });
            items.Add(new TableData { Key = 2, Value = "2" });
            table.Execute(Command.Insert(null, items));

            Assert.AreEqual(2, table.TriggerInsertCount);

            // Insert
            items.Clear();
            items.Add(new TableData { Key = 2, Value = "new_2" });
            items.Add(new TableData { Key = 3, Value = "new_3" });
            table.Execute(Command.Merge(null, items));

            var result = table.Select(x => x).ToArray();
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(1, result.Count(x => x.Key == 2 && x.Value == "new_2"));
            Assert.AreEqual(1, result.Count(x => x.Key == 3 && x.Value == "new_3"));
        }

        [TestMethod]
        public void TableCumulativeUpdateTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, ValueInt = -1 });
            items.Add(new TableData { Key = 2, ValueInt = 0 });
            items.Add(new TableData { Key = 3, ValueInt = 1 });
            table.Execute(Command.Insert(null, items));

            Assert.AreEqual(3, table.TriggerInsertCount);

            // Insert
            items.Clear();
            items.Add(new TableData { Key = 1, ValueInt = 5 });
            items.Add(new TableData { Key = 2, ValueInt = 6 });
            table.Execute(Command.Update(null, items, "@ValueInt"));

            var result = table.Select(x => x).ToArray();

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(1, result.Count(x => x.Key == 1 && x.ValueInt == 4));
            Assert.AreEqual(1, result.Count(x => x.Key == 2 && x.ValueInt == 6));
            Assert.AreEqual(1, result.Count(x => x.Key == 3 && x.ValueInt == 1));
        }

        [TestMethod]
        public void TableTriggerInsertTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1" });
            items.Add(new TableData { Key = 2, Value = "2" });
            items.Add(new TableData { Key = 3, Value = "3" });
            table.Execute(Command.Insert(null, items));

            Assert.AreEqual(3, table.TriggerInsertCount);

            // Insert
            table.TriggerCountClear();
            items.Clear();
            items.Add(new TableData { Key = 4, Value = "4" });
            items.Add(new TableData { Key = 5, Value = "5" });
            table.Execute(Command.Insert(null, items));

            Assert.AreEqual(2, table.TriggerInsertCount);
            Assert.AreEqual(0, table.TriggerUpdateCount);
            Assert.AreEqual(0, table.TriggerDeleteCount);
        }

        [TestMethod]
        public void TableTriggerUpdateTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1" });
            items.Add(new TableData { Key = 2, Value = "2" });
            items.Add(new TableData { Key = 3, Value = "3" });
            table.Execute(Command.Insert(null, items ));

            // Update
            table.TriggerCountClear();
            items.Clear();
            items.Add(new TableData { Key = 2, Value = "new_2" });
            items.Add(new TableData { Key = 3, Value = "3" });
            items.Add(new TableData { Key = 4, Value = "4" });
            table.Execute(Command.Update(null, items));

            Assert.AreEqual(0, table.TriggerInsertCount);
            Assert.AreEqual(1, table.TriggerUpdateCount);
            Assert.AreEqual(0, table.TriggerDeleteCount);

        }

        [TestMethod]
        public void TableTriggerDeleteTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1" });
            items.Add(new TableData { Key = 2, Value = "2" });
            items.Add(new TableData { Key = 3, Value = "3" });
            table.Execute(Command.Insert(null, items));

            // Delete
            table.TriggerCountClear();
            items.Clear();
            items.Add(new TableData { Key = 2 });
            items.Add(new TableData { Key = 3 });
            table.Execute(Command.Delete(null, items));

            Assert.AreEqual(0, table.TriggerInsertCount);
            Assert.AreEqual(0, table.TriggerUpdateCount);
            Assert.AreEqual(2, table.TriggerDeleteCount);
        }

        [TestMethod]
        public void TableTriggerMergeTest()
        {
            var table = new TableTest();
            var items = new List<TableData>();

            // Init data
            items.Add(new TableData { Key = 1, Value = "1" });
            items.Add(new TableData { Key = 2, Value = "2" });
            items.Add(new TableData { Key = 3, Value = "3" });
            table.Execute(Command.Insert(null, items));

            // Merge
            table.TriggerCountClear();
            items.Clear();
            items.Add(new TableData { Key = 2, Value = "2" });
            items.Add(new TableData { Key = 3, Value = "new_3" });
            items.Add(new TableData { Key = 4, Value = "4" });
            items.Add(new TableData { Key = 5, Value = "5" });
            table.Execute(Command.Merge(null, items));

            Assert.AreEqual(2, table.TriggerInsertCount);
            Assert.AreEqual(1, table.TriggerUpdateCount);
            Assert.AreEqual(1, table.TriggerDeleteCount);
        }
    }
}
