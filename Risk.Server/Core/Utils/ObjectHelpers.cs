using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public static class ObjectHelpers
    {
        public static string ToString(this object obj, PropertyInfo[] properties, bool withNames = false)
        {
            if (withNames)
                return String.Join(", ", (from p in properties select p.Name + " = " + (p.GetValue(obj) ?? (object)"<null>").ToString()));
            else
                return String.Join(", ", (from p in properties select (p.GetValue(obj) ?? (object)"<null>").ToString()));
        }

        public static bool InheritedFrom(this Type sourceType, Type type)
        {
            while (sourceType != null && sourceType != typeof(object))
            {
                var cur = sourceType.IsGenericType ? sourceType.GetGenericTypeDefinition() : sourceType;
                if (type == cur)
                {
                    return true;
                }
                sourceType = sourceType.BaseType;
            }
            return false;
        }

        public static T CloneObject<T>(this T source, bool fullCopy = false)
            where T: new()
        {
            if (source is ICloneable)
                return (T)((ICloneable)source).Clone();

            // TODO: ??? Cache expressions

            Type typeSource = source.GetType();
            T destination = new T();

            PropertyInfo[] propertyInfo = typeSource.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (PropertyInfo property in propertyInfo)
            {
                if (property.CanWrite)
                {
                    if (!fullCopy || property.PropertyType.IsValueType || property.PropertyType.IsEnum || property.PropertyType.Equals(typeof(System.String)))
                    {
                        property.SetValue(destination, property.GetValue(source, null), null);
                    }
                    else
                    {
                        object objPropertyValue = property.GetValue(source, null);
                        if (objPropertyValue == null)
                        {
                            property.SetValue(destination, null, null);
                        }
                        else
                        {
                            property.SetValue(destination, objPropertyValue.CloneObject(fullCopy), null);
                        }
                    }
                }
            }
            return destination;
        }

        public static TResult CloneObject<T, TResult>(this T source, bool fullCopy = false)
            where TResult : new()
        {
            Type typeSource = source.GetType();
            TResult destination = new TResult();

            PropertyInfo[] propertyInfoSource = typeSource.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo[] propertyInfoDestination = typeof(TResult).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (PropertyInfo property in propertyInfoDestination)
            {
                if (property.CanWrite)
                {
                    var propertySource = propertyInfoSource.FirstOrDefault(p => p.Name == property.Name && p.PropertyType == property.PropertyType);
                    if (propertySource == null)
                        continue;

                    if (!fullCopy || property.PropertyType.IsValueType || property.PropertyType.IsEnum || property.PropertyType.Equals(typeof(System.String)))
                    {
                        property.SetValue(destination, propertySource.GetValue(source, null), null);
                    }
                    else
                    {
                        object objPropertyValue = propertySource.GetValue(source, null);
                        if (objPropertyValue == null)
                        {
                            property.SetValue(destination, null, null);
                        }
                        else
                        {
                            property.SetValue(destination, objPropertyValue.CloneObject(fullCopy), null);
                        }
                    }
                }
            }
            return destination;
        }

        public static TResult ConvertType<T, TResult>(this T x)
            where T : class
            where TResult : new()
        {
            // IConvertible
            if (x as IConvertible != null)
                return (TResult)Convert.ChangeType(x, typeof(TResult));

            // Implicit conversion
            var conversionMethod = typeof(T).GetMethod("op_Implicit", new[] { typeof(T) });
            if (conversionMethod != null)
                return (TResult)conversionMethod.Invoke(null, new[] { x });

            // Copy object (slowest method)
            return x.CloneObject<T, TResult>();
        }
    }
}
