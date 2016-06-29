using Rebus.Config;
using Rebus.Serialization;

namespace Rebus.MsgPack
{
    /// <summary>
    /// Configuration extensions for the Jil serializer
    /// </summary>
    public static class MsgPackConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use the simple and really fast MsgPack serializer
        /// </summary>
        public static void UseMsgPack(this StandardConfigurer<ISerializer> configurer)
        {
            configurer.Register(c => new MsgPackSerializer());
        }
    }
}