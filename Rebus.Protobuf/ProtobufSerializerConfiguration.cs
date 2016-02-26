using ProtoBuf;
using ProtoBuf.Meta;
using Rebus.Config;
using Rebus.Serialization;

namespace Rebus.Protobuf
{
    /// <summary>
    /// Configuration extensions for the Rebus Protobuf serializer
    /// </summary>
    public static class ProtobufSerializerConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use the Protobuf serializer with the default protobuf-net settings (i.e. with the <see cref="RuntimeTypeModel.Default"/> instance
        /// of the <see cref="RuntimeTypeModel"/>, requiring you to either decorate your type with appropriate <see cref="ProtoMemberAttribute"/> or
        /// supplying appropriate metadata to the default instance)
        /// </summary>
        public static void UseProtobuf(this StandardConfigurer<ISerializer> configurer)
        {
            configurer.Register(c => new ProtobufSerializer());
        }

        /// <summary>
        /// Configures Rebus to use the Protobuf serializer with the given <see cref="RuntimeTypeModel"/>, requiring you to either decorate your type with 
        /// appropriate <see cref="ProtoMemberAttribute"/> or supplying appropriate metadata to the instance passed in)
        /// </summary>
        public static void UseProtobuf(this StandardConfigurer<ISerializer> configurer, RuntimeTypeModel runtimeTypeModel)
        {
            configurer.Register(c => new ProtobufSerializer(runtimeTypeModel));
        }
    }
}