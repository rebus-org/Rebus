using Rebus.Serialization.Binary;
using Rebus.Serialization.Json;

namespace Rebus.Configuration
{
    public class RebusSerializationConfigurer
    {
        readonly ConfigurationBackbone backbone;

        public RebusSerializationConfigurer(ConfigurationBackbone backbone)
        {
            this.backbone = backbone;
        }


        public void UseJsonSerializer()
        {
            backbone.SerializeMessages = new JsonMessageSerializer();
        }

        public void UseBinarySerializer()
        {
            backbone.SerializeMessages = new BinaryMessageSerializer();
        }
    }
}