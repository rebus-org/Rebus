using System.Messaging;
using Newtonsoft.Json;
using Rebus.Serialization.Binary;
using Rebus.Serialization.Json;

namespace Rebus.Configuration
{
    /// <summary>
    /// Configurer that allows for configuring which implementation of <see cref="ISerializeMessages"/> that should be used
    /// </summary>
    public class RebusSerializationConfigurer : BaseConfigurer
    {
        internal RebusSerializationConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
        }

        /// <summary>
        /// Configures Rebus to use <see cref="JsonMessageSerializer"/> to serialize messages. A <see cref="JsonSerializationOptions"/>
        /// object is returned, which can be used to configure details around how the JSON serialization should work
        /// </summary>
        public JsonSerializationOptions UseJsonSerializer()
        {
            var jsonMessageSerializer = new JsonMessageSerializer();
            Use(jsonMessageSerializer);
            return new JsonSerializationOptions(jsonMessageSerializer);
        }

        /// <summary>
        /// Configures Rebus to use <see cref="BinaryMessageSerializer"/> which internally uses the BCL <see cref="BinaryMessageFormatter"/>
        /// to serialize messages
        /// </summary>
        public void UseBinarySerializer()
        {
            Use(new BinaryMessageSerializer());
        }

        /// <summary>
        /// Uses the specified implementation of <see cref="ISerializeMessages"/> to serialize transport messages
        /// </summary>
        public void Use(ISerializeMessages serializer)
        {
            Backbone.SerializeMessages = serializer;
        }
    }
}