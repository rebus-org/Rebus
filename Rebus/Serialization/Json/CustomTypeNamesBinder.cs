using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Rebus.Serialization.Json
{
    /// <summary>
    /// Newtonsoft JSON.NET serialization binder backed by <see cref="DefaultSerializationBinder"/>, but with the ability to customize the type names
    /// used for specific types.
    /// </summary>
    public class CustomTypeNamesBinder : DefaultSerializationBinder
    {
        readonly ConcurrentDictionary<Type, string> TypeToName = new ConcurrentDictionary<Type, string>();
        readonly ConcurrentDictionary<string, Type> NameToType = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// Indicates whether the binder is allowed to fall back to default JSON.NET behavior
        /// </summary>
        bool _allowFallback;

        /// <summary>
        /// Gets the <paramref name="assemblyName"/> and <paramref name="typeName"/> for the given <paramref name="serializedType"/>
        /// </summary>
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (TypeToName.TryGetValue(serializedType, out var value))
            {
                assemblyName = null;
                typeName = value;
                return;
            }

            if (!_allowFallback)
            {
                throw new ArgumentException($@"Cannot bind type {serializedType} to a type name, because only the following types have been mapped:

{string.Join(Environment.NewLine, TypeToName.Select(kvp => $"    {kvp.Key} = {kvp.Value}"))}

and fallback to default JSON.NET behavior is not allowed. 

Either enable fallback by calling .AllowFallbackToDefaultBehavior() on the binder, or add the type {serializedType} with one of the .AddWith(...) methods.");
            }

            base.BindToName(serializedType, out assemblyName, out typeName);
        }

        /// <summary>
        /// Gets the type that corresponds to the given <paramref name="assemblyName"/> and <paramref name="typeName"/>
        /// </summary>
        public override Type BindToType(string assemblyName, string typeName)
        {
            var typeNameToLookUp = string.IsNullOrWhiteSpace(assemblyName)
                ? typeName
                : $"{typeName}, {assemblyName}";

            if (NameToType.TryGetValue(typeNameToLookUp, out var type))
            {
                return type;
            }

            if (!_allowFallback)
            {
                throw new ArgumentException($@"Cannot bind assembly name {assemblyName} and type name {typeName} to a type, because only the following types have been mapped:

{string.Join(Environment.NewLine, TypeToName.Select(kvp => $"    {kvp.Key} = {kvp.Value}"))}

and fallback to default JSON.NET behavior is not allowed. 

Either enable fallback by calling .AllowFallbackToDefaultBehavior() on the binder, or add a type for the name '{typeNameToLookUp}' with one of the .AddWith(...) methods.");
            }

            return base.BindToType(assemblyName, typeName);
        }

        /// <summary>
        /// Gets a builder that can be used to configure this particular binder instance
        /// </summary>
        public CustomTypeNamesBinderBuilder GetBuilder() => new CustomTypeNamesBinderBuilder(TypeToName, NameToType, () => _allowFallback = true);
    }
}