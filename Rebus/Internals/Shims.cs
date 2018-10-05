// ReSharper disable RedundantUsingDirective
using System;
using System.Reflection;
using System.Text;

namespace Rebus.Internals
{
    /// <summary>
    /// Internal shims - compiler hints on the form #if NETSTANDARD1_3 etc should be moved here
    /// </summary>
    static class Shims
    {
        public static Assembly GetAssembly(this Type type)
        {
            return type.Assembly;
        }

        /// <summary>
        /// Gets whether the type is generic
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        public static StringBuilder BuildSimpleAssemblyQualifiedName(Type type, StringBuilder sb)
        {
            if (!type.IsGenericType)
            {
                sb.Append($"{type.FullName}, {type.Assembly.GetName().Name}");
                return sb;
            }

            if (!type.IsConstructedGenericType)
            {
                return sb;
            }

            var fullName = type.FullName;
            var requiredPosition = fullName.IndexOf("[", StringComparison.Ordinal);
            var name = fullName.Substring(0, requiredPosition);
            sb.Append($"{name}[");

            var arguments = type.GetGenericArguments();
            for (var i = 0; i < arguments.Length; i++)
            {
                sb.Append(i == 0 ? "[" : ", [");
                BuildSimpleAssemblyQualifiedName(arguments[i], sb);
                sb.Append("]");
            }

            sb.Append($"], {type.Assembly.GetName().Name}");

            return sb;
        }

    }
}