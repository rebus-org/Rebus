// ReSharper disable RedundantUsingDirective
using System;
using System.Text;

namespace Rebus.Internals;

/// <summary>
/// Internal shims - compiler hints on the form #if NETSTANDARD1_3 etc should be moved here
/// </summary>
static class Shims
{
    public static string GetSimpleAssemblyQualifiedName(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        var simpleAssemblyQualifiedName = BuildSimpleAssemblyQualifiedName(type, new StringBuilder()).ToString();

        // type lookups apparently accept "mscorlib" as an alias for System.Private.CoreLib, so we can make the "simple assembly-qualified type name" consistent across platforms like this
        return simpleAssemblyQualifiedName.Replace("System.Private.CoreLib", "mscorlib");
    }

    static StringBuilder BuildSimpleAssemblyQualifiedName(Type type, StringBuilder sb)
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

        var fullName = type.FullName ?? "???";
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