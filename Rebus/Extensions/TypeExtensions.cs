using System;
using System.Collections.Generic;
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
            foreach (var implementedInterface in type.GetTypeInfo().GetInterfaces())
            {
                yield return implementedInterface;
            }

            while (type.GetTypeInfo().BaseType != null)
            {
                var baseType = type.GetTypeInfo().BaseType;
                yield return baseType;
                type = baseType;
            }
        }

        /// <summary>
        /// Gets the assembly-qualified name of the type, without any version info etc.
        /// E.g. "System.String, mscorlib"
        /// </summary>
        public static string GetSimpleAssemblyQualifiedName(this Type type)
        {
            return $"{type.FullName}, {type.GetTypeInfo().Assembly.GetName().Name}";
        }
    }
}