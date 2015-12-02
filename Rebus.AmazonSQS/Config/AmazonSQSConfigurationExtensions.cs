using Amazon;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Timeouts;
using Rebus.Transport;

namespace Rebus.AmazonSQS.Config
{
    /// <summary>
    /// Configuration extensions for the Amazon Simple Queue Service transport
    /// </summary>
    public static class AmazonSqsConfigurationExtensions
    {
        const string SqsTimeoutManagerText = "A disabled timeout manager was installed as part of the SQS configuration, becuase the transport has native support for deferred messages";

        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSqs(this StandardConfigurer<ITransport> configurer, string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint, string inputQueueAddress)
        {
            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();

                return new AmazonSqsTransport(inputQueueAddress, accessKeyId, secretAccessKey, regionEndpoint, rebusLoggerFactory);
            });

            configurer
                .OtherService<IPipeline>()
                .Decorate(p =>
                {
                    var pipeline = p.Get<IPipeline>();

                    return new PipelineStepRemover(pipeline)
                        .RemoveIncomingStep(s => s.GetType() == typeof (HandleDeferredMessagesStep));
                });

            configurer.OtherService<ITimeoutManager>().Register(c => new DisabledTimeoutManager(), description: SqsTimeoutManagerText);
        }

        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSqsAsOneWayClient(this StandardConfigurer<ITransport> configurer, string accessKeyId, string secretAccessKey, RegionEndpoint regionEndpoint)
        {
            configurer.Register(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();

                return new AmazonSqsTransport(null, accessKeyId, secretAccessKey, regionEndpoint, rebusLoggerFactory);
            });

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }
    }
}
