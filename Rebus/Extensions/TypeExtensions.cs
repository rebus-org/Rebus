using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Rebus.Internals;

namespace Rebus.Extensions;

/// <summary>
/// Provides extensions of <see cref="Type"/>
/// </summary>
public static class TypeExtensions
{
    static readonly ConcurrentDictionary<Type, string> SimpleAssemblyQualifiedTypeNameCache = new();

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

        return SimpleAssemblyQualifiedTypeNameCache.GetOrAdd(type, Shims.GetSimpleAssemblyQualifiedName);
    }
}