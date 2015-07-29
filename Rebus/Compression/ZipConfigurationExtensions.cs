using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

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
        public static OptionsConfigurer EnableCompression(this OptionsConfigurer configurer,
            int bodySizeThresholdBytes = DefaultBodyThresholdBytes)
        {
            configurer.Register(c => new Zipper());
            configurer.Register(c => new UnzipMessagesIncomingStep(c.Get<Zipper>()));
            configurer.Register(c => new ZipMessagesOutgoingStep(c.Get<Zipper>(), bodySizeThresholdBytes));

            configurer.Decorate<IPipeline>(c => new PipelineStepInjector(c.Get<IPipeline>())
                .OnReceive(c.Get<UnzipMessagesIncomingStep>(), PipelineRelativePosition.Before, typeof(DeserializeIncomingMessageStep))
                .OnSend(c.Get<ZipMessagesOutgoingStep>(), PipelineRelativePosition.After, typeof(SerializeOutgoingMessageStep)));

            return configurer;
        }
    }
}