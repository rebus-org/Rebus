using System;
using System.Collections.Generic;

namespace Rebus.Extensions
{
    public static class TypeExtensions
    {
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
    }
}