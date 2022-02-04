using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rebus.Serialization.Custom;

/// <summary>
/// Builder for configuring <see cref="CustomTypeNameConvention"/>
/// </summary>
public class CustomTypeNameConventionBuilder
{
    readonly ConcurrentDictionary<Type, string> _typeToName = new();
    readonly ConcurrentDictionary<string, Type> _nameToType = new();

    bool _allowFallback;

    /// <summary>
    /// Adds all of the given <paramref name="types"/> with their "short names",
    /// i.e. without assembly or namespace information. Please note that nested classes have names that include their parent classes.
    /// </summary>
    public CustomTypeNameConventionBuilder AddWithShortNames(IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            AddWithShortName(type);
        }
        return this;
    }

    /// <summary>
    /// Adds the type <typeparamref name="T"/> with its "short name",
    /// i.e. without assembly or namespace information. Please note that nested classes have names that include their parent classes.
    /// </summary>
    public CustomTypeNameConventionBuilder AddWithShortName<T>() => AddWithShortName(typeof(T));

    /// <summary>
    /// Adds the type <paramref name="type"/> with its "short name",
    /// i.e. without assembly or namespace information. Please note that nested classes have names that include their parent classes.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public CustomTypeNameConventionBuilder AddWithShortName(Type type) => AddWithCustomName(type, type.Name);

    /// <summary>
    /// Adds the type <typeparamref name="T"/> with the name <paramref name="name"/>.
    /// </summary>
    public CustomTypeNameConventionBuilder AddWithCustomName<T>(string name) => AddWithCustomName(typeof(T), name);

    /// <summary>
    /// Adds the type <paramref name="type"/> with the name <paramref name="name"/>.
    /// </summary>
    public CustomTypeNameConventionBuilder AddWithCustomName(Type type, string name)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (name == null) throw new ArgumentNullException(nameof(name));

        if (_typeToName.ContainsKey(type))
        {
            throw new ArgumentException($"Cannot add known type {type} named '{name}', because the type was alread added with the name '{_typeToName[type]}'");
        }

        if (_nameToType.ContainsKey(name))
        {
            throw new ArgumentException($"Cannot add known type {type} named '{name}', because the name was alread added for the type '{_nameToType[name]}'");
        }

        AddMapping(type, name);
        AddMapping(type.MakeArrayType(), $"{name}[]");

        return this;
    }

    /// <summary>
    /// Allows for falling back to the default behavior in cases where type mappings have not been added explicitly
    /// </summary>
    public CustomTypeNameConventionBuilder AllowFallbackToDefaultConvention()
    {
        _allowFallback = true;
        return this;
    }

    internal CustomTypeNameConvention GetConvention() => new(_typeToName, _nameToType, _allowFallback);

    void AddMapping(Type type, string name)
    {
        _typeToName[type] = name;
        _nameToType[name] = type;
    }
}