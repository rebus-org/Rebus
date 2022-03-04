using System;
using System.Linq.Expressions;

namespace Rebus.Tests.Extensions;

class Reflect
{
    public static string Path<T>(Expression<Func<T, object>> expression)
    {
        return GetPropertyName(expression);
    }

    public static object Value(object obj, string path)
    {
        var dots = path.Split('.');

        foreach(var dot in dots)
        {
            var propertyInfo = obj.GetType().GetProperty(dot);
            if (propertyInfo == null) return null;
            obj = propertyInfo.GetValue(obj, new object[0]);
            if (obj == null) break;
        }

        return obj;
    }

    static string GetPropertyName(Expression expression)
    {
        if (expression == null) return "";

        if (expression is LambdaExpression)
        {
            expression = ((LambdaExpression) expression).Body;
        }

        if (expression is UnaryExpression)
        {
            expression = ((UnaryExpression)expression).Operand;
        }

        if (expression is MemberExpression)
        {
            dynamic memberExpression = expression;

            var lambdaExpression = (Expression)memberExpression.Expression;

            string prefix;
            if (lambdaExpression != null)
            {
                prefix = GetPropertyName(lambdaExpression);
                if (!string.IsNullOrEmpty(prefix))
                {
                    prefix += ".";
                }
            }
            else
            {
                prefix = "";
            }

            var propertyName = memberExpression.Member.Name;
                
            return prefix + propertyName;
        }

        return "";
    }
}