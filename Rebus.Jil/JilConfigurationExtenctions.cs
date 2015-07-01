using Rebus.Config;
using Rebus.Serialization;

namespace Rebus.Jil
{
    /// <summary>
    /// Configuration extensions for the Jil serializer
    /// </summary>
    public static class JilConfigurationExtenctions
    {
        /// <summary>
        /// Configures Rebus to use the simple and extremely fast Jil JSON serializer
        /// </summary>
        public static void UseJil(this StandardConfigurer<ISerializer> configurer)
        {
            configurer.Register(c => new JilSerializer());
        }
    }
}