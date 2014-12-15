﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Runtime.Serialization;

namespace Risk
{
    /// <summary>
    /// Базовый объект данных
    /// </summary>
    public abstract class DataObjectBase<T> : IDataObject
        where T : class, new()
    {
        
        private int _refreshTime = 0;
        private Timer timerUpdateChanges;

        public string Name { get; private set; }

        public Type ObjectType 
        {
            get { return typeof(T); }
        }

        public int RefreshTime
        {
            get
            {
                return _refreshTime;
            }
            set
            {
                _refreshTime = value;
                if (timerUpdateChanges != null)
                    timerUpdateChanges.Dispose();
                if (_refreshTime != 0)
                    timerUpdateChanges = new Timer(c => NotifyChanges(), null, _refreshTime, _refreshTime);
            }
        }

        public DataObjectBase()
        {
            object[] attrs = GetType().GetCustomAttributes(typeof(DataObjectAttribute), true);
            if (attrs.Length == 1)
            {
                Name = ((DataObjectAttribute)attrs[0]).Name;
            }
        }

        public virtual FieldInfo[] GetFields(ParameterCollection parameters)
        {
            if (Attribute.GetCustomAttribute(typeof(T), typeof(DataContractAttribute)) == null)
                return GetFields(parameters, Properties);
            else
                return GetFields(parameters, Properties.Where(x => x.GetCustomAttribute(typeof(DataMemberAttribute)) != null).ToArray());
        }

        public virtual FieldInfo[] GetFields(ParameterCollection parameters, PropertyInfo[] properties)
        {
            var fields = new List<FieldInfo>();
            foreach (var property in properties)
            {
                fields.Add(new FieldInfo
                {
                    FieldName = property.Name,
                    ReadOnly = !property.CanWrite,
                    DataType = FieldInfo.GetFieldTypeCode(property.PropertyType),
                    // TODO: ??? Size
                });
            }
            return fields.ToArray();
        }

        protected void CheckFields<TItem>(PropertyInfo[] properties)
        {
            // Check KeyFields if DataContract
            if (Attribute.GetCustomAttribute(typeof(TItem), typeof(DataContractAttribute)) != null)
            {
                var errorProp = properties.FirstOrDefault(x => x.GetCustomAttribute(typeof(DataMemberAttribute)) == null);
                if (errorProp != null)
                    throw new Exception(String.Format("Key field property '{0}' table '{1}' has no attribute [DataMember], but class '{2}' has attrubute [DataContract]", errorProp.Name, Name, typeof(T).Name));
            }
            // TODO: Check string field !!! throw new Exception(String.Format("String key field '{0}' type '{1}' must be set StringLength attribute", prop.Name, "unknown"));
        }

        public abstract object GetData(ParameterCollection parameters);

        public abstract void SetData(ParameterCollection parameters, object data);

        protected virtual NotificationData<T> GetChanges()
        {
            return null;
        }

        protected virtual Expression<Func<T, bool>> Predicate(ParameterCollection parameters)
        {
            return null;
        }

        public object CreateDataFromParams(ParameterCollection parameters, bool updateFields)
        {
            var data = new T();
            var sb = new StringBuilder();
            foreach (Parameter parameter in parameters)
            {
                if (parameter.Name.StartsWith("[") && parameter.Name.EndsWith("]") && parameter.Value != null)
                {
                    var property = Properties.FirstOrDefault(x => String.Equals("[" + x.Name + "]", parameter.Name, StringComparison.InvariantCultureIgnoreCase));
                    if (property == null)
                        throw new Exception(String.Format("Invalid field '{0}' in parameters for object type '{1}'", parameter.Name, typeof(T).GetType().Name));

                    if (parameter.Value.GetType() != property.PropertyType)
                    {
                        if (property.PropertyType.IsEnum)
                            property.SetValue(data, Enum.ToObject(property.PropertyType, parameter.Value));
                        else
                            property.SetValue(data, Convert.ChangeType(parameter.Value, property.PropertyType));
                    }
                    else
                        property.SetValue(data, parameter.Value);
                    if (UpdatedProperties.Contains(property))
                    {
                        if (sb.Length > 0)
                            sb.Append(",");
                        sb.Append(property.Name);
                    }
                }
            }
            if (sb.Length > 0)
            {
                if (updateFields)
                    parameters["Fields"] = sb.ToString();
                return data;
            }
            else
                return null;
        }

        #region Properties

        protected virtual PropertyInfo[] GetProperties()
        {
            return (from p in typeof(T).GetProperties()
                    where p.CanRead
                    select p).ToArray();
        }

        private PropertyInfo[] _properties;
        protected PropertyInfo[] Properties
        {
            get
            {
                if (_properties == null)
                    _properties = GetProperties();
                return _properties;
            }
        }

        // TODO: ??? Cache fieldNames
        protected virtual PropertyInfo[] GetProperties(string propertyNames)
        {
            if (String.IsNullOrWhiteSpace(propertyNames))
                return Properties;

            var fields = new List<string>();
            var propertiesList = new List<PropertyInfo>();
            foreach (var fieldStr in propertyNames.Split(','))
            {
                var fieldName = fieldStr.Trim();

                if (fields.Contains(fieldName))
                    throw new Exception(String.Format("Doublicate field name '{0}' object '{1}'", fieldName, Name));

                var property = Properties.FirstOrDefault(p => p.Name == fieldName);
                if (property == null)
                    throw new Exception(String.Format("Invalid field '{0}' object '{1}'", fieldName, Name));

                propertiesList.Add(property);
            }
            return propertiesList.ToArray();
        }

        protected virtual PropertyInfo[] GetUpdatedProperties()
        {
            return (from p in typeof(T).GetProperties()
                    where p.CanWrite
                    select p).ToArray();
        }

        private PropertyInfo[] _updatedProperties;
        protected PropertyInfo[] UpdatedProperties
        {
            get
            {
                if (_updatedProperties == null)
                    _updatedProperties = GetUpdatedProperties();
                return _updatedProperties;
            }
        }

        // TODO: ??? Cache fieldNames
        protected virtual void UpdatedPropertiesFromNames(string propertyNames, out PropertyInfo[] properties, out PropertyInfo[] cumulativeProperties, out PropertyInfo[] ignoreProperties)
        {
            properties = UpdatedProperties;
            cumulativeProperties = null;
            ignoreProperties = null;

            if (String.IsNullOrWhiteSpace(propertyNames))
                return;

            var fields = new List<string>();
            var propertiesList = new List<PropertyInfo>();
            var cumulativePropertiesList = new List<PropertyInfo>();
            var ignorePropertiesList = new List<PropertyInfo>(UpdatedProperties);
            foreach (var fieldStr in propertyNames.Split(','))
            {
                var fieldName = fieldStr.Trim();
                bool isCumulativeField = fieldName.StartsWith("@");
                if (isCumulativeField)
                    fieldName = fieldName.Substring(1);

                if (fields.Contains(fieldName))
                    throw new Exception(String.Format("Doublicate field name {0} in commnd for table '{1}'", fieldName, Name));

                var property = UpdatedProperties.FirstOrDefault(p => p.Name == fieldName);
                if (property == null)
                    throw new Exception(String.Format("Invalid update field '{0}' for object '{1}'", fieldName, Name));

                if (isCumulativeField)
                    cumulativePropertiesList.Add(property);
                else
                    propertiesList.Add(property);
                ignorePropertiesList.Remove(property);
            }
            properties = propertiesList.ToArray();
            cumulativeProperties = cumulativePropertiesList.ToArray();
            ignoreProperties = ignorePropertiesList.ToArray();
        }

        protected bool UpdateInstance(TriggerPair<T> instancePair, PropertyInfo[] properties, PropertyInfo[] cumulativeProperties)
        {
            if (instancePair.Deleted == null || instancePair.Inserted == null)
                return false;

            bool result = cumulativeProperties != null && cumulativeProperties.Count() > 0;

            // Update
            if (properties != null)
                foreach (var prop in properties)
                {
                    var oldValue = prop.GetValue(instancePair.Deleted, null);
                    var newValue = prop.GetValue(instancePair.Inserted, null);

                    if (!object.Equals(oldValue, newValue))
                    {
                        prop.SetValue(instancePair.Updated, newValue, null);
                        result = true;
                    }
                }

            // Cumulative update
            if (cumulativeProperties != null)
                foreach (var prop in cumulativeProperties)
                {
                    var oldValue = prop.GetValue(instancePair.Deleted, null);
                    var newValue = prop.GetValue(instancePair.Inserted, null);

                    if (prop.PropertyType == typeof(decimal))
                        newValue = (decimal)oldValue + (decimal)newValue;
                    else if (prop.PropertyType == typeof(int))
                        newValue = (int)oldValue + (int)newValue;
                    else
                        throw new Exception(String.Format("Type '{0}' not supported cumulative update", prop.PropertyType.Name));

                    prop.SetValue(instancePair.Updated, newValue, null);
                    prop.SetValue(instancePair.Inserted, newValue, null);
                }

            return result;
        }

        #endregion

        #region Notification

        public virtual void AddNotification(Connection connection, string correlationId, ParameterCollection parameters)
        {
            // Add notification
            var predicate = Predicate(parameters);
            var notification = new Notification(connection)
            {
                CorrelationId = correlationId,
                DataObject = this,
                Predicate = predicate == null ? null : predicate.Compile(),
            };
            connection.Notifications.AddOrUpdate(correlationId, notification, (key, oldValue) => oldValue = notification);
        }

        public void RemoveNotification(Connection connection, string correlationId)
        {
            if (connection == null)
                throw new Exception("For unsubscribe command must be has connection");
            Notification removeNotification;
            connection.Notifications.TryRemove(correlationId, out removeNotification);
        }

        private void NotifyChanges()
        {
            // Get changes
            var changes = GetChanges();
            if (changes == null || changes.IsEmpty)
                return;
            
            // Create notifications list
            var notifications = (from connection in Server.Connections
                                 from notification in connection.Notifications.Values
                                 where notification.DataObject == this
                                 select notification).ToList();
            if (notifications.Count == 0)
                return;

            // Notify
            foreach (var notification in notifications)
                NotifyChanges(notification, changes);
        }

        protected virtual void NotifyChanges<TResult>(Notification notification, NotificationData<TResult> notificationData)
            where TResult : class, new()
        {
            if (notificationData.Created != null && notificationData.Created.Length > 0)
                notification.Notify(NotificationType.Create, notificationData.Created);

            if (notificationData.Inserted != null && notificationData.Inserted.Length > 0)
                notification.Notify(NotificationType.Insert, notificationData.Inserted);

            if (notificationData.Updated != null && notificationData.Updated.Length > 0)
                notification.Notify(NotificationType.Update, (from item in notificationData.Updated select item.Inserted).ToArray());

            if (notificationData.Deleted != null && notificationData.Deleted.Length > 0)
                notification.Notify(NotificationType.Delete, notificationData.Deleted);
        }

        #endregion  
    }
}