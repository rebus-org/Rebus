using Rebus.Config;
using Rebus.Serialization;

namespace Rebus.Compression
{
    /// <summary>
    /// Configuration extensions for enabling compression
    /// </summary>
    public static class ZipConfigurationExtensions
    {
        /// <summary>
        /// Default threshold for the body size for compression to kick in
        /// </summary>
        public const int DefaultBodyThresholdBytes = 1024;

        /// <summary>
        /// Enables compression of outgoing messages if the size exceeds the specified number of bytes
        /// (defaults to <see cref="DefaultBodyThresholdBytes"/>)
        /// </summary>
        public static void EnableCompression(this OptionsConfigurer configurer, int bodySizeThresholdBytes = DefaultBodyThresholdBytes)
        {
            configurer.Decorate<ISerializer>(c => new ZippingSerializerDecorator(c.Get<ISerializer>(), new Zipper(), bodySizeThresholdBytes));
        }
    }
}