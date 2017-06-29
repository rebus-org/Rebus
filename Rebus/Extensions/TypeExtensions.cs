using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Rebus.Extensions
{
    /// <summary>
    /// Provides extensions of <see cref="Type"/>
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets the type's base types (i.e. the <see cref="Type"/> for each implemented interface and for each class inherited from, all the way up to <see cref="Object"/>)
        /// </summary>
        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            foreach (var implementedInterface in type.GetInterfaces())
            {
                yield return implementedInterface;
            }

            while (type.GetTypeInfo().BaseType != null)
            {
                yield return type.GetTypeInfo().BaseType;
                type = type.GetTypeInfo().BaseType;
            }
        }

        /// <summary>
        /// Gets the assembly-qualified name of the type, without any version info etc.
        /// E.g. "System.String, mscorlib"
        /// </summary>
        public static string GetSimpleAssemblyQualifiedName(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return BuildSimpleAssemblyQualifiedName(type, new StringBuilder()).ToString();
        }

        static StringBuilder BuildSimpleAssemblyQualifiedName(Type type, StringBuilder sb)
        {
#if NETSTANDARD1_3
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                sb.Append($"{type.FullName}, {typeInfo.Assembly.GetName().Name}");
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

            sb.Append($"], {typeInfo.Assembly.GetName().Name}");

            return sb;
#else
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
#endif
        }
    }
}