using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Risk
{
    /// <summary>
    /// Набор расширений для выражений
    /// </summary>
    public static class ExpressionsRoutines
    {
        public static Type CreateAnymouseType<T>(params string[] fields)
        {
            AssemblyName dynamicAssemblyName = new AssemblyName("AnymouseTypes");
            AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(dynamicAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule("AnymouseTypes");
            TypeBuilder dynamicAnonymousType = dynamicModule.DefineType("Key", TypeAttributes.Public);
            foreach (var fieldName in fields)
                dynamicAnonymousType.DefineField(fieldName, typeof(T).GetProperty(fieldName).PropertyType, FieldAttributes.Public);
            return dynamicAnonymousType.CreateType();
        }

        public static Func<T, object> CreateSelectorExpression<T>(PropertyInfo[] properties)
        {
            if (properties == null || properties.Length == 0)
                return x => x;

            if (properties.Count() == 1)
            {
                var param = Expression.Parameter(typeof(T), "x");
                var propValues = Expression.Property(param, properties[0]);
                var selector = Expression.Lambda<Func<T, object>>(Expression.TypeAs(propValues, typeof(object)), param);
                return selector.Compile();
            }
            else
            {
                var param = Expression.Parameter(typeof(T), "x");
                var paramExpression = new List<Expression>();
                var paramTypes = new List<Type>();
                foreach (var property in properties)
                {
                    paramExpression.Add(Expression.Property(param, property));
                    paramTypes.Add(property.PropertyType);
                }
                var selector = Expression.Lambda<Func<T, object>>(Expression.Call(typeof(Tuple), "Create", paramTypes.ToArray(), paramExpression.ToArray()), param);
                return selector.Compile();
            }
        }

        public static Expression<Func<T, bool>> CreatePredicateParamsExpression<T>(ParameterCollection parameters, PropertyInfo[] properties)
        {
            Expression<Func<T, bool>> expression = null;

            // Filter
            string filter = (string)parameters["Filter"];
            if (!String.IsNullOrWhiteSpace(filter))
            {
                expression = System.Linq.Dynamic.DynamicExpression.ParseLambda<T, bool>(filter);
            }

            // Field params
            int paramIndex = 0;
            ParameterExpression par = Expression.Parameter(typeof(T), "");
            while (paramIndex < parameters.Count)
            {
                var parameter = parameters.GetParameter(paramIndex);
                var property = properties.FirstOrDefault(x => String.Equals("[" + x.Name + "]", parameter.Name, StringComparison.InvariantCultureIgnoreCase));

                if (property != null)
                {
                    Expression keyFieldValues = Expression.Property(par, property);
                    Expression paramValue = Expression.Constant(parameter.Value);

                    BinaryExpression comparison = Expression.Equal(keyFieldValues, paramValue);
                    var paramExpression = Expression.Lambda<Func<T, bool>>(comparison, par);

                    if (expression != null)
                        expression = expression.And(paramExpression);
                    else
                        expression = paramExpression;
                }
                else if (parameter.Name.StartsWith("[") && parameter.Name.EndsWith("]"))
                    throw new Exception(String.Format("Invalid field '{0}' in parameters for type '{1}'", parameter.Name, typeof(T).Name));
                paramIndex++;
            }
            return expression ?? Expression.Lambda<Func<T, bool>>(Expression.Constant(true), par);
        }

        public static Expression<Func<T, bool>> CreatePredicateParamsExpression<T, TResult>(ParameterCollection parameters, PropertyInfo[] properties)
        {
            var par = Expression.Parameter(typeof(T), "");
            var createMeth = typeof(ObjectHelpers).GetMethod("ConvertType");
            var method = createMeth.MakeGenericMethod(typeof(T), typeof(TResult));
            var fff = Expression.Call(method, par);
            return Expression.Lambda<Func<T, bool>>(Expression.Invoke(CreatePredicateParamsExpression<TResult>(parameters, properties), fff), par);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> thisExpression, Expression<Func<T, bool>> expression)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(Expression.Invoke(thisExpression, thisExpression.Parameters), Expression.Invoke(expression, thisExpression.Parameters)), thisExpression.Parameters);
        }
    }
}
