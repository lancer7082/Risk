using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Risk
{
    // TODO: ??? View based on Linq
    public class Table<T, TResult> : Table<T>
        where T : class, new()
        where TResult : class, new()
    {
        public string ResultKeyFieldNames { get; private set; }

        public Table()
            : base()
        {
            object[] attrs = GetType().GetCustomAttributes(typeof(TableResultAttribute), true);
            if (attrs.Length == 1)
            {
                var resultKeyFields = ((TableResultAttribute)attrs[0]).ResultKeyFields;
                if (!String.IsNullOrWhiteSpace(resultKeyFields))
                {
                    ResultKeyFieldNames = ((TableResultAttribute)attrs[0]).ResultKeyFields;
                }
            }

            if (String.IsNullOrWhiteSpace(ResultKeyFieldNames))
                ResultKeyFieldNames = KeyFieldNames;

            var errorPropName = (from keyFieldName in ResultKeyFieldNames.Split(',')
                                 where ResultProperties.FirstOrDefault(x => x.Name == (keyFieldName ?? "").Trim()) == null
                                 select keyFieldName).FirstOrDefault();
            if (errorPropName != null)
                throw new Exception(String.Format("Key field '{0}' not found in Result type'{1}' for table '{2}'", errorPropName, typeof(TResult).Name, Name));

            //  CheckFields<TResult>(ResultKeyFieldNames);
        }

        protected PropertyInfo[] GetResultProperties()
        {
            return (from p in typeof(TResult).GetProperties()
                    where p.CanRead
                    select p).ToArray();
        }

        private PropertyInfo[] _resultProperties;
        protected PropertyInfo[] ResultProperties
        {
            get
            {
                if (_resultProperties == null)
                    _resultProperties = GetResultProperties();
                return _resultProperties;
            }
        }

        public override FieldInfo[] GetFields(ParameterCollection parameters)
        {
            var fields = base.GetFields(parameters, ResultProperties);

            if (String.IsNullOrWhiteSpace(ResultKeyFieldNames))
                return fields;

            var resultKeyFieldNames = (from fieldName in ResultKeyFieldNames.Split(',')
                                       select fieldName.Trim()).ToArray();

            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].IsKey = resultKeyFieldNames.FirstOrDefault(fieldName => fieldName == fields[i].FieldName) != null;
            }
            return fields;
        }

        //public override object GetData(ParameterCollection parameters)
        //{
        //    IEnumerable<T> items = this;
        //    var predicate = Predicate(parameters);
        //    if (predicate != null)
        //        items = this.Where(predicate.Compile());

        //    return items.Select(x => x.ConvertType<T, TResult>()).ToArray();
        //}

        public override object GetData(ParameterCollection parameters)
        {
            var items = this.Select(x => x.ConvertType<T, TResult>());

            var predicate = PredicateResult(parameters);
            if (predicate != null)
                items = items.Where(predicate.Compile());
            return items.ToArray();
        }

        protected override Expression<Func<T, bool>> Predicate(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

        protected virtual Expression<Func<TResult, bool>> PredicateResult(ParameterCollection parameters)
        {
            return ExpressionsRoutines.CreatePredicateParamsExpression<TResult, bool>(parameters, ResultProperties);
        }

        public override void AddNotification(Connection connection, string correlationId, ParameterCollection parameters)
        {
            // Add notification
            var predicate = PredicateResult(parameters); // ExpressionsRoutines.CreatePredicateParamsExpression<TResult>(parameters, ResultProperties);
            var notification = new Notification(connection)
            {
                CorrelationId = correlationId,
                DataObject = this,
                Predicate = predicate == null ? null : predicate.Compile(),
            };
            connection.Notifications.AddOrUpdate(correlationId, notification, (key, oldValue) => oldValue = notification);
        }

        protected override void NotifyChanges<TItem>(Notification notification, NotificationData<TItem> notificationData)
        {
            var notificationDataResult = new NotificationData<TResult>();
            notificationDataResult.Created = notificationData.Created == null ? null : notificationData.Created.Select(x => x.ConvertType<TItem, TResult>()).ToArray();
            notificationDataResult.Inserted = notificationData.Inserted == null ? null : notificationData.Inserted.Select(x => x.ConvertType<TItem, TResult>()).ToArray();
            notificationDataResult.Updated = notificationData.Updated == null ? null : notificationData.Updated.Select(x => new TriggerPair<TResult>(x.Inserted.ConvertType<TItem, TResult>(), x.Deleted.ConvertType<TItem, TResult>())).ToArray();
            notificationDataResult.Deleted = notificationData.Deleted == null ? null : notificationData.Deleted.Select(x => x.ConvertType<TItem, TResult>()).ToArray();
            base.NotifyChanges<TResult>(notification, notificationDataResult);
        }
    }
}