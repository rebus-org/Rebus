using System;
using System.Collections.Generic;

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

            while (type.BaseType != null)
            {
                yield return type.BaseType;
                type = type.BaseType;
            }
        }

        /// <summary>
        /// Gets the assembly-qualified name of the type, without any version info etc.
        /// E.g. "System.String, mscorlib"
        /// </summary>
        public static string GetSimpleAssemblyQualifiedName(this Type type)
        {
            return string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
        }
    }
}