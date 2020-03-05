using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;

namespace Rebus.Serialization.Json
{
    /// <summary>
    /// Builder for configuring <see cref="CustomTypeNamesBinder"/>
    /// </summary>
    public class CustomTypeNamesBinderBuilder
    {
        readonly ConcurrentDictionary<Type, string> _typeToName;
        readonly ConcurrentDictionary<string, Type> _nameToType;
        readonly Action _allowFallbackSet;

        internal CustomTypeNamesBinderBuilder(ConcurrentDictionary<Type, string> typeToName, ConcurrentDictionary<string, Type> nameToType, Action allowFallbackSet)
        {
            _typeToName = typeToName;
            _nameToType = nameToType;
            _allowFallbackSet = allowFallbackSet;
        }

        /// <summary>
        /// Adds all of the given <paramref name="types"/> with their "short names",
        /// i.e. without assembly or namespace information. Please note that nested classes have names that include their parent classes.
        /// </summary>
        public CustomTypeNamesBinderBuilder AddWithShortNames(IEnumerable<Type> types)
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
        public CustomTypeNamesBinderBuilder AddWithShortName<T>() => AddWithShortName(typeof(T));

        /// <summary>
        /// Adds the type <paramref name="type"/> with its "short name",
        /// i.e. without assembly or namespace information. Please note that nested classes have names that include their parent classes.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public CustomTypeNamesBinderBuilder AddWithShortName(Type type) => AddWithCustomName(type, type.Name);

        /// <summary>
        /// Adds the type <typeparamref name="T"/> with the name <paramref name="name"/>.
        /// </summary>
        public CustomTypeNamesBinderBuilder AddWithCustomName<T>(string name) => AddWithCustomName(typeof(T), name);

        /// <summary>
        /// Adds the type <paramref name="type"/> with the name <paramref name="name"/>.
        /// </summary>
        public CustomTypeNamesBinderBuilder AddWithCustomName(Type type, string name)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (!_typeToName.TryAdd(type, name))
            {
                throw new ArgumentException($"Cannot add known type {type} named '{name}', because the type was alread added with the name '{_typeToName[type]}'");
            }

            if (!_nameToType.TryAdd(name, type))
            {
                throw new ArgumentException($"Cannot add known type {type} named '{name}', because the name was alread added for the type '{_nameToType[name]}'");
            }

            return this;
        }

        /// <summary>
        /// Enables falling back to default JSON.NET behavior, meaning that types not explicitly mapped in this binder will be handled by JSON.NET's <see cref="DefaultSerializationBinder"/>
        /// </summary>
        public CustomTypeNamesBinderBuilder AllowFallbackToDefaultBehavior()
        {
            _allowFallbackSet();
            return this;
        }
    }
}