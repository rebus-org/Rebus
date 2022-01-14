using System;
using System.Collections.Concurrent;
using Rebus.Extensions;

namespace Rebus.Serialization;

class SimpleAssemblyQualifiedMessageTypeNameConvention : IMessageTypeNameConvention
{
    readonly ConcurrentDictionary<Type, string> _typeToName = new ConcurrentDictionary<Type, string>();
    readonly ConcurrentDictionary<string, Type> _nameToType = new ConcurrentDictionary<string, Type>();

    public string GetTypeName(Type type) => _typeToName.GetOrAdd(type, _ => type.GetSimpleAssemblyQualifiedName());

    public Type GetType(string name) => _nameToType.GetOrAdd(name, _ => Type.GetType(name));
}