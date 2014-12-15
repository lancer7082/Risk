using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using NLog;

namespace Risk
{
    /// <summary>
    /// Базовый класс таблицы
    /// </summary>
    public class Table<T> : DataObjectBase<T>, ITable, IEnumerable<T>, IEnumerable
        where T : class, new()
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        protected object updateLock = new object();
        private List<T> _items = new List<T>();
        private IEnumerable<T> _cacheData;
        private IList<Expression<Func<T, object>>> _lookups;
        private Dictionary<string, ILookup<object, T>> _indexes;

        // Changes lists
        protected List<T> ChangedInserted { get; private set; }
        protected Dictionary<T, T> ChangedUpdated { get; private set; }
        protected List<T> ChangedDeleted { get; private set; }

        protected IEnumerable<T> Items
        {
            get
            {
                // TODO: ??? Interlocked.MemoryBarrier();
                if (_cacheData == null)
                {
                    lock (updateLock)
                    {
                        _cacheData = _items.Select(item => (T)item.CloneObject()).ToArray();
                    }
                }
                return _cacheData;
            }
        }

        public string KeyFieldNames { get; private set; }

        protected PropertyInfo[] KeyFieldProperties { get; private set; }
        protected virtual Func<T, object> KeySelector { get; private set; }

        public Table()
        {
            object[] attrs = GetType().GetCustomAttributes(typeof(TableAttribute), true);
            if (attrs.Length == 1)
            {
                var keyFields = ((TableAttribute)attrs[0]).KeyFields;
                if (!String.IsNullOrWhiteSpace(keyFields))
                {
                    KeyFieldNames = ((TableAttribute)attrs[0]).KeyFields;
                }
            }

            if (String.IsNullOrWhiteSpace(KeyFieldNames))
                throw new Exception(String.Format("KeyFields empty for Table '{0}'", Name));

            KeyFieldProperties = GetProperties(KeyFieldNames);
            CheckFields<T>(KeyFieldProperties);
            KeySelector = ExpressionsRoutines.CreateSelectorExpression<T>(KeyFieldProperties);

            if (KeySelector == null)
                KeySelector = x => x;

            _lookups = new List<Expression<Func<T, object>>>();
            _indexes = new Dictionary<string, ILookup<object, T>>();

            ChangedInserted = new List<T>();
            ChangedUpdated = new Dictionary<T, T>();
            ChangedDeleted = new List<T>();
        }

        public override FieldInfo[] GetFields(ParameterCollection parameters, PropertyInfo[] properties)
        {
            var fields = base.GetFields(parameters, properties);

            if (String.IsNullOrWhiteSpace(KeyFieldNames))
                return fields;

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].IsKey = KeyFieldProperties.FirstOrDefault(p => p.Name == fields[i].FieldName) != null;
            }
            return fields;
        }

        /// <summary>
        ///  Trigger on Add, Update, Delete
        /// </summary>
        public virtual void TriggerAfter(TriggerCollection<T> items)
        {
        }

        #region Indexes

        public void AddIndex(Expression<Func<T, object>> property)
        {
            _lookups.Add(property);
            _indexes.Add(property.ToString(), _items.ToLookup(property.Compile()));
        }

        public void Clear()
        {
            lock (updateLock)
            {
                _items.Clear();
                Interlocked.Exchange(ref _cacheData, null);
            }
        }

        public void Add(T item)
        {
            lock (updateLock)
            {
                _items.Add(item);
                Interlocked.Exchange(ref _cacheData, null);
                // RebuildIndexes();
            }
        }

        public void Remove(T item)
        {
            lock (updateLock)
            {
                _items.Remove(item);
                Interlocked.Exchange(ref _cacheData, null);
                // RebuildIndexes();
            }
        }

        public void RebuildIndexes()
        {
            if (_lookups.Count > 0)
            {
                _indexes = new Dictionary<string, ILookup<object, T>>();
                foreach (var lookup in _lookups)
                {
                    _indexes.Add(lookup.ToString(), _items.ToLookup(lookup.Compile()));
                }
            }
        }

        public IEnumerable<T> FindValue<TProperty>(Expression<Func<T, TProperty>> property, TProperty value)
        {
            var key = property.ToString();
            if (_indexes.ContainsKey(key))
            {
                return _indexes[key][value];
            }
            else
            {
                var c = property.Compile();
                return _items.Where(x => object.Equals(c(x), value));
            }
        }

        #endregion

        #region IEnumerator

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region ObjectData

        protected override PropertyInfo[] GetUpdatedProperties()
        {
            return base.GetUpdatedProperties().Except(KeyFieldProperties).ToArray();
        }

        public override object GetData(ParameterCollection parameters)
        {
            IEnumerable<T> result = this;
            var predicate = Predicate(parameters);
            if (predicate != null)
                result = this.Where(predicate.Compile());

            return result.ToArray();
        }

        public override void SetData(ParameterCollection parameters, object data)
        {
            Update(PrepareData(data), (string)parameters["Fields"]);
        }

        private IEnumerable<T> PrepareData(object data)
        {
            if (data == null)
                return null;

            if (data is IEnumerable<T>)
                return (IEnumerable<T>)data;

            if (data is T)
                return new T[] { (T)data };

            var dataType = data.GetType();
            if (data is IEnumerable)
            {
                var items = data as IEnumerable<T>;
                if (items != null)
                    return items;

                // TODO: ??? Cast anymouse type
                //var iEnumerable = data.GetType().GetInterface(typeof(IEnumerable<>).Name);
                //if (iEnumerable != null)
                //{
                //    dataType = iEnumerable.GetGenericArguments()[0];
                //}

                return ((IEnumerable)data).Cast<dynamic>().Select(x => (T)x);
            }

            throw new Exception(String.Format("Data for table '{0}' unsupported type '{1}'", Name, dataType.Name));
        }

        protected override NotificationData<T> GetChanges()
        {
            lock (updateLock)
            {
                if (ChangedDeleted.Count == 0 && ChangedUpdated.Count == 0 && ChangedInserted.Count == 0)
                    return null;

                var notificationData = new NotificationData<T>();
                if (String.IsNullOrWhiteSpace(KeyFieldNames)) // If KeyFieldNames not exists, update all items
                {
                    notificationData.Created = Items.ToArray();
                }
                else
                {
                    if (ChangedDeleted.Count > 0)
                        notificationData.Deleted = ChangedDeleted.ToArray();

                    if (ChangedUpdated.Count > 0)
                        notificationData.Updated = (from item in ChangedUpdated select new TriggerPair<T>(item.Key, item.Value)).ToArray();

                    if (ChangedInserted.Count > 0)
                        notificationData.Inserted = ChangedInserted.ToArray();
                }

                ChangedDeleted.Clear();
                ChangedUpdated.Clear();
                ChangedInserted.Clear();

                return notificationData;
            }
        }

        protected override void NotifyChanges<TResult>(Notification notification, NotificationData<TResult> notificationData)
        {
            if (notification.Predicate == null)
            {
                base.NotifyChanges(notification, notificationData);
            }
            else
            {
                var predicateFunc = (Func<TResult, bool>)notification.Predicate;

                var insertedItems = notificationData.Inserted == null ? new List<TResult>() : notificationData.Inserted.Where(predicateFunc).ToList();
                var deletedItems = notificationData.Deleted == null ? new List<TResult>() : notificationData.Deleted.Where(predicateFunc).ToList();
                var updatedtems = new List<TriggerPair<TResult>>();

                if (notificationData.Updated != null)
                {

                    insertedItems.AddRange(from item in notificationData.Updated
                                           where !predicateFunc(item.Deleted)
                                              && predicateFunc(item.Inserted)
                                           select item.Inserted);

                    deletedItems.AddRange(from item in notificationData.Updated
                                          where predicateFunc(item.Deleted)
                                             && !predicateFunc(item.Inserted)
                                          select item.Deleted);

                    updatedtems.AddRange(from item in notificationData.Updated
                                         where predicateFunc(item.Deleted)
                                            && predicateFunc(item.Inserted)
                                         select item);
                }

                notificationData = new NotificationData<TResult>
                {
                    Inserted = insertedItems.ToArray(),
                    Updated = updatedtems.ToArray(),
                    Deleted = deletedItems.ToArray(),
                };

                if (!notificationData.IsEmpty)
                    base.NotifyChanges(notification, notificationData);
            }
        }

        protected override Expression<Func<T, bool>> Predicate(ParameterCollection parameters)
        {
            return ExpressionsRoutines.CreatePredicateParamsExpression<T>(parameters, Properties);
        }

        #endregion

        #region Actions

        protected internal virtual void Insert(IEnumerable<T> items)
        {
            if (items == null)
                throw new Exception(String.Format("Add data for table '{0}' is empty", Name));

            // execute LINQ
            items = items.ToArray();
            if (!items.Any())
                return;

            // Check constraints
            var errorConstraintInput = (items.GroupBy(KeySelector)
                                      .Where(x => x.Count() > 1)
                                      .Select(x => x.FirstOrDefault())).FirstOrDefault();

            if (errorConstraintInput != null)
                throw new Exception(String.Format("Add data for table '{0}' duplicate key '{1}'. The duplicate key value is ({2})", Name, KeyFieldNames, errorConstraintInput.ToString(KeyFieldProperties)));

            lock (updateLock)
            {
                var updated = items.GroupJoin(_items, KeySelector, KeySelector, (i, g) => g // left join
                    .Select(d => new TriggerPair<T>(i, d))
                    .DefaultIfEmpty(new TriggerPair<T>(i, null)))
                    .SelectMany(x => x).ToArray();

                // Check constraints
                var errorConstraint = updated.FirstOrDefault(p => p.Deleted != null);
                if (errorConstraint != null)
                    throw new Exception(String.Format("Cannot insert duplicate key '{0}' in table '{1}'. The duplicate key value is ({2})", KeyFieldNames, Name, errorConstraint.Inserted.ToString(KeyFieldProperties)));

                // TODO: ??? Before trigger

                var addItems = updated.Select(x => x.Updated);
                _items.AddRange(addItems);

                // Update changes lists
                foreach (var item in updated)
                {
                    if (ChangedDeleted.Contains(item.Updated))
                        ChangedDeleted.Remove(item.Updated);
                    ChangedInserted.Add(item.Updated);
                }

                // Trigger after
                if (updated.Length > 0)
                    TriggerAfter(new TriggerCollection<T>(updated));

                Interlocked.Exchange(ref _cacheData, null);
            }
        }

        protected internal virtual void Update(IEnumerable<T> items, string fieldNames)
        {
            if (items == null)
                throw new Exception(String.Format("Update data for table '{0}' is empty", Name));

            // execute LINQ
            items = items.ToArray();
            if (!items.Any())
                return;

            PropertyInfo[] properties;
            PropertyInfo[] cumulativeProperties;
            PropertyInfo[] ignoreProperties;
            UpdatedPropertiesFromNames(fieldNames, out properties, out cumulativeProperties, out ignoreProperties);

            lock (updateLock)
            {

                var updated = items.Join(_items, KeySelector, KeySelector, (i, d) => new TriggerPair<T>(i, d, ignoreProperties)).ToList();

                // TODO: ??? Before trigger

                for (int i = updated.Count - 1; i >= 0; i--)
                {
                    var item = updated[i];

                    if (UpdateInstance(item, properties, cumulativeProperties))
                    {
                        // Update changes lists
                        if (!ChangedInserted.Contains(item.Updated) && !ChangedUpdated.ContainsKey(item.Updated))
                            ChangedUpdated.Add(item.Updated, item.Deleted);
                    }
                    else
                        updated.RemoveAt(i);
                }

                if (updated.Count > 0)
                    TriggerAfter(new TriggerCollection<T>(updated));

                Interlocked.Exchange(ref _cacheData, null);
            }
        }

        protected internal virtual void Delete(IEnumerable<T> items)
        {
            if (items == null)
                throw new Exception(String.Format("Delete data for table '{0}' is empty", Name));

            // execute LINQ
            items = items.ToArray();
            if (!items.Any())
                return;

            lock (updateLock)
            {
                var updated = items.Join(_items, KeySelector, KeySelector, (i, d) => new TriggerPair<T>(null, d)).ToArray();

                // TODO: ??? Before trigger

                foreach (var item in updated)
                {
                    _items.Remove(item.Updated);

                    // Update changes lists
                    if (ChangedInserted.Contains(item.Updated))
                        ChangedInserted.Remove(item.Updated);
                    if (ChangedUpdated.ContainsKey(item.Updated))
                        ChangedUpdated.Remove(item.Updated);
                    ChangedDeleted.Add(item.Updated);
                }

                if (updated.Length > 0)
                    TriggerAfter(new TriggerCollection<T>(updated));

                Interlocked.Exchange(ref _cacheData, null);
            }
        }

        protected internal virtual void Merge(IEnumerable<T> items, string fieldNames, string keyFieldNames)
        {
            if (items == null)
                throw new Exception(String.Format("Merge data for table '{0}' is empty", Name));

            PropertyInfo[] properties;
            PropertyInfo[] cumulativeProperties;
            PropertyInfo[] ignoreProperties;
            UpdatedPropertiesFromNames(fieldNames, out properties, out cumulativeProperties, out ignoreProperties);

            // execute LINQ
            items = items.ToArray();
            if (!items.Any())
                return;

            // Check constraint arguments
            var errorConstraintInput = (items.GroupBy(KeySelector)
                                      .Where(x => x.Count() > 1)
                                      .Select(x => x.FirstOrDefault())).FirstOrDefault();

            if (errorConstraintInput != null)
                throw new Exception(String.Format("Add data for table '{0}' duplicate key '{1}'. The duplicate key value is ({2})", Name, KeyFieldNames, errorConstraintInput.ToString(KeyFieldProperties)));

            var mergeSelector = String.IsNullOrWhiteSpace(keyFieldNames) ? KeySelector : ExpressionsRoutines.CreateSelectorExpression<T>(GetProperties(keyFieldNames));

            lock (updateLock)
            {
                IEnumerable<T> mergeItems;
                IEnumerable<object> allKeys;

                if (!String.IsNullOrWhiteSpace(keyFieldNames))
                {
                    var mergeKeys = items.Select(mergeSelector).Distinct();
                    mergeItems = mergeKeys.Join(_items, x => x, mergeSelector, (i, d) => d);
                    allKeys = mergeItems.Select(KeySelector).Union(items.Select(KeySelector));
                }
                else
                {
                    mergeItems = _items;
                    allKeys = _items.Select(KeySelector).Union(items.Select(KeySelector));
                }

                var updated = allKeys.GroupJoin(items, x => x, KeySelector, (k, gk) => new { k, gk })
                    .SelectMany(x => x.gk.DefaultIfEmpty(), (g, i) => new { g, i })
                    .GroupJoin(mergeItems, x => x.g.k, KeySelector, (gi, gd) => new { gi, gd })
                    .SelectMany(x => x.gd.DefaultIfEmpty(), (g, d) => new TriggerPair<T>(g.gi.i, d, ignoreProperties)).ToList();

                // TODO: ??? Before trigger

                for (int i = updated.Count - 1; i >= 0; i--)
                {
                    var item = updated[i];

                    // Insert
                    if (item.Deleted == null)
                    {
                        _items.Add(item.Updated);

                        if (ChangedDeleted.Contains(item.Updated))
                            ChangedDeleted.Remove(item.Updated);
                        ChangedInserted.Add(item.Updated);
                    }
                    // Delete
                    else if (item.Inserted == null)
                    {
                        _items.Remove(item.Updated);

                        // Update changes lists
                        if (ChangedInserted.Contains(item.Updated))
                            ChangedInserted.Remove(item.Updated);
                        if (ChangedUpdated.ContainsKey(item.Updated))
                            ChangedUpdated.Remove(item.Updated);
                        ChangedDeleted.Add(item.Updated);
                    }
                    // Update
                    else if (!UpdateInstance(item, properties, cumulativeProperties))
                    {
                        updated.RemoveAt(i);
                    }
                    else
                    {
                        // Update changes lists
                        if (!ChangedInserted.Contains(item.Updated) && !ChangedUpdated.ContainsKey(item.Updated))
                            ChangedUpdated.Add(item.Updated, item.Deleted);
                    }
                }

                if (updated.Count > 0)
                    TriggerAfter(new TriggerCollection<T>(updated));

                Interlocked.Exchange(ref _cacheData, null);
            }
        }

        #endregion

        #region ITable
        void ITable.Insert(object data)
        {
            Insert(PrepareData(data));
        }

        void ITable.Update(object data, string fieldNames)
        {
            Update(PrepareData(data), fieldNames);
        }

        void ITable.Delete(object data)
        {
            Delete(PrepareData(data));
        }

        void ITable.Merge(object data, string fieldNames, string keyFieldNames)
        {
            Merge(PrepareData(data), fieldNames, keyFieldNames);
        }
        #endregion
    }
}

// Indexer - Index large collections by different keys on memory or disk
// http://www.codeproject.com/Articles/563200/Indexer-Index-large-collections-by-different-keys

// How to check if two Expression<Func<T, bool>> are the same [duplicate]
// http://stackoverflow.com/questions/673205/how-to-check-if-two-expressionfunct-bool-are-the-same/673246#673246

// Хитрый способ кэширования Expression Trees
// http://lambdy.ru/post/9869744363/expression-trees