using System.Text;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Serialization;
using Rebus.Transport;
using Rebus.Transport.Msmq;

namespace Rebus.Legacy
{
    /// <summary>
    /// Configuration extensions for enabling legacy compatibility
    /// </summary>
    public static class LegacyCompatibilityConfigurationExtensions
    {
        /// <summary>
        /// Makes Rebus "legacy compatible", i.e. enables wire-level compatibility with older Rebus versions. WHen this is enabled,
        /// all endpoints need to be old Rebus endpoints or new Rebus endpoints with this feature enabled
        /// </summary>
        public static void EnableLegacyCompatibility(this OptionsConfigurer configurer)
        {
            configurer.Decorate(c =>
            {
                var pipeline = c.Get<IPipeline>();

                // map headers of incoming message from v1 to v2
                pipeline = new PipelineStepConcatenator(pipeline)
                    .OnReceive(new MapLegacyHeadersIncomingStep(), PipelineAbsolutePosition.Front);

                // unpack object[] of transport message
                pipeline = new PipelineStepInjector(pipeline)
                    .OnReceive(new UnpackLegacyMessageIncomingStep(), PipelineRelativePosition.After, typeof (DeserializeIncomingMessageStep));

                // pack into object[]
                pipeline = new PipelineStepInjector(pipeline)
                    .OnSend(new PackLegacyMessageOutgoingStep(), PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));

                pipeline = new PipelineStepInjector(pipeline)
                    .OnSend(new MapLegacyHeadersOutgoingStep(), PipelineRelativePosition.Before, typeof(SendOutgoingMessageStep));

                return pipeline;
            });

            configurer.Register<ISerializer>(c =>
            {
                var defaultLegacyEncoding = Encoding.UTF7;

                return new JsonSerializer(defaultLegacyEncoding);
            });

            configurer.Decorate(c =>
            {
                var transport = c.Get<ITransport>();

                if (transport is MsmqTransport)
                {
                    ((MsmqTransport) transport).UseLegacyHeaderSerialization();
                }

                return transport;
            });
        }
    }
}