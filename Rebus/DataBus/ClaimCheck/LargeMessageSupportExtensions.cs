using Rebus.Compression;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;

namespace Rebus.DataBus.ClaimCheck
{
    /// <summary>
    /// Configuration extensions for Rebus' automatic big message claim check feature.
    /// </summary>
    public static class LargeMessageSupportExtensions
    {
        /// <summary>
        /// <para>
        /// Configures Rebus to automatically transfer message bodies as data bus attachments, if the size of the body exceeds <paramref name="messageSizeThresholdBytes"/> bytes.
        /// </para>
        /// <para>
        /// This can be used in situations when you know that the message size will sometimes be too big for the transport, like e.g. when using Azure Service Bus.
        /// With Azure Service Bus (at least at the time of writing), the maximum message size is 256 kB, including all headers and everything.
        /// </para>
        /// <para>
        /// Since it can be hard to predict how large the final Azure Service Bus transport message can get, if you know that your message payloads will approach the 256 kB limit,
        /// it is recommended to enable automatic compression of message payloads (by calling <see cref="ZipConfigurationExtensions.EnableCompression"/>).
        /// </para>
        /// <para>
        /// If you still fear that your message payloads will approach the limit, this feature is for you :) simply ensure that the data bus is properly configured
        /// (e.g. to use Azure Blob Storage to store attachments), and then call this method to enable automatic big message claim check.
        /// </para>
        /// </summary>
        public static void AutomaticallySendBigMessagesAsAttachments(this OptionsConfigurer configurer, int messageSizeThresholdBytes)
        {
            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var dataBus = c.Get<IDataBus>();

                var dehydrateStep = new DehydrateOutgoingMessageStep(dataBus, messageSizeThresholdBytes);
                var hydrateStep = new HydrateIncomingMessageStep(dataBus);

                return new PipelineStepInjector(pipeline)
                    .OnSend(
                        step: dehydrateStep,
                        position: PipelineRelativePosition.After,
                        anchorStep: typeof(SerializeOutgoingMessageStep)
                    )
                    .OnReceive(
                        step: hydrateStep,
                        position: PipelineRelativePosition.Before,
                        anchorStep: typeof(DeserializeIncomingMessageStep)
                    );
            });
        }

    }
}