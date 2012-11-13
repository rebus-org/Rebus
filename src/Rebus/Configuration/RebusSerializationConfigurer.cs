using System;
using Rebus.Serialization.Binary;
using Rebus.Serialization.Json;

namespace Rebus.Configuration
{
    public class RebusSerializationConfigurer : BaseConfigurer
    {
        public RebusSerializationConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        public JsonSerializationOptions UseJsonSerializer()
        {
            var jsonMessageSerializer = new JsonMessageSerializer();
            Backbone.SerializeMessages = jsonMessageSerializer;
            return new JsonSerializationOptions(jsonMessageSerializer);
        }

        public void UseBinarySerializer()
        {
            Backbone.SerializeMessages = new BinaryMessageSerializer();
        }

        public void Use(ISerializeMessages serializer)
        {
            Backbone.SerializeMessages = serializer;
        }
    }

    public class JsonSerializationOptions
    {
        readonly JsonMessageSerializer jsonMessageSerializer;

        public JsonSerializationOptions(JsonMessageSerializer jsonMessageSerializer)
        {
            this.jsonMessageSerializer = jsonMessageSerializer;
        }

        public JsonSerializationOptions AddNameResolver(Func<Type, TypeDescriptor> resolve)
        {
            jsonMessageSerializer.AddNameResolver(resolve);
            return this;
        }

        public JsonSerializationOptions AddTypeResolver(Func<TypeDescriptor, Type> resolve)
        {
            jsonMessageSerializer.AddTypeResolver(resolve);
            return this;
        }
    }
}