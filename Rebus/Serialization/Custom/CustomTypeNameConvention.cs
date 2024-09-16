using System;
using System.Collections.Concurrent;
using System.Linq;
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.Serialization.Custom;

sealed class CustomTypeNameConvention : IMessageTypeNameConvention
{
    static string CodeExample(Type type = null, string name = null)
    {
        string TypeText() => type?.Name ?? "SomeType";
        string NameText() => name ?? "SomeTypeName";

        return $@"Configure.With(...)
    .(...)
    .Serialization(s => {{
        s.UseCustomMessageTypeNames()
            .(...)
            .AddWithCustomName<{TypeText()}>(""{NameText()}"");
    }})
    .Start();";
    }

    readonly SimpleAssemblyQualifiedMessageTypeNameConvention _defaultConvention = new();
    readonly ConcurrentDictionary<Type, string> _typeToName;
    readonly ConcurrentDictionary<string, Type> _nameToType;
    readonly bool _allowFallback;

    public CustomTypeNameConvention(ConcurrentDictionary<Type, string> typeToName, ConcurrentDictionary<string, Type> nameToType, bool allowFallback)
    {
        _typeToName = typeToName;
        _nameToType = nameToType;
        _allowFallback = allowFallback;
    }

    public string GetTypeName(Type type)
    {
        return _typeToName.TryGetValue(type, out var result)
            ? result
            : _allowFallback
                ? _defaultConvention.GetTypeName(type)
                : throw new ArgumentException(
                    $@"Cannot get type name for {type}, because only the following types have been mapped:

{GetListOfMappedTypes()}

Please add the type {type} with one of the .AddWith(...) methods in the serialization configuration, e.g. like so:

{CodeExample(type: type)}

or enable fallback to the default convention by calling .AllowFallbackToDefaultConvention() on the builder.
");

    }

    public Type GetType(string name)
    {
        return _nameToType.TryGetValue(name, out var result)
            ? result
            : _allowFallback
                ? _defaultConvention.GetType(name)
                : throw new ArgumentException(
                    $@"Cannot get type corresponding to the name '{name}', because only the following types have been mapped:

{GetListOfMappedTypes()}

Please add a type with the name '{name}' with one of the .AddWith(...) methods in the serialization configuration, e.g. like so:

{CodeExample(name: name)}

or enable fallback to the default convention by calling .AllowFallbackToDefaultConvention() on the builder.
");
    }

    string GetListOfMappedTypes()
    {
        return _typeToName.Any()
            ? string.Join(Environment.NewLine, _typeToName.Select(kvp => $"    {kvp.Key} = '{kvp.Value}'"))
            : "(none)";
    }
}