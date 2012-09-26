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

        public void UseJsonSerializer()
        {
            Backbone.SerializeMessages = new JsonMessageSerializer();
        }

        public void UseBinarySerializer()
        {
            Backbone.SerializeMessages = new BinaryMessageSerializer();
        }

        public void Use(ISerializeMessages serializer)
        {
            backbone.SerializeMessages = serializer;
        }
    }
}