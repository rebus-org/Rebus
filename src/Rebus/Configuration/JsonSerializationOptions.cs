using System;
using Rebus.Serialization.Json;

namespace Rebus.Configuration
{
    /// <summary>
    /// Allows for configuring details around how JSON serialization should work
    /// </summary>
    public class JsonSerializationOptions
    {
        readonly JsonMessageSerializer jsonMessageSerializer;

        internal JsonSerializationOptions(JsonMessageSerializer jsonMessageSerializer)
        {
            this.jsonMessageSerializer = jsonMessageSerializer;
        }

        /// <summary>
        /// Adds a function that will determine how a given type is turned into a <see cref="TypeDescriptor"/>.
        /// Return null if the function has no opinion about this particular type, allowing other functions and
        /// ultimately the default JSON serializer's opinion to be used.
        /// </summary>
        public JsonSerializationOptions AddNameResolver(Func<Type, TypeDescriptor> resolve)
        {
            jsonMessageSerializer.AddNameResolver(resolve);
            return this;
        }

        /// <summary>
        /// Adds a function that will determine how a given <see cref="TypeDescriptor"/> is turned into a .NET type.
        /// Return null if the function has no opinion about this particular type descriptor, allowing other functions and
        /// ultimately the default JSON serializer's opinion to be used.
        /// </summary>
        public JsonSerializationOptions AddTypeResolver(Func<TypeDescriptor, Type> resolve)
        {
            jsonMessageSerializer.AddTypeResolver(resolve);
            return this;
        }
    }
}