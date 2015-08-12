using Amazon;
using Rebus.Config;
using Rebus.Transport;

namespace Rebus.AmazonSQS.Config
{
    /// <summary>
    /// Configuration extensions for the Amazon Simple Queue Service transport
    /// </summary>
    public static class AmazonSqsConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use Amazon Simple Queue Service as the message transport
        /// </summary>
        public static void UseAmazonSqs(this StandardConfigurer<ITransport> configurer, string accessKeyId, string secretAccessKey,  RegionEndpoint regionEndpoint, string inputQueueAddress)
        {
            configurer.Register(c => new AmazonSqsTransport(inputQueueAddress, accessKeyId, secretAccessKey, regionEndpoint));
        }
    }
}
