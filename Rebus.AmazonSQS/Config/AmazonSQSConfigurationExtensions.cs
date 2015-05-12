using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Rebus.Config;
using Rebus.Transport;

namespace Rebus.AmazonSQS.Config
{
    public static class AmazonSqsConfigurationExtensions
    {
        public static void UseAmazonSqs(this StandardConfigurer<ITransport> configurer, string accessKeyId, string secretAccessKey,  RegionEndpoint regionEndpoint, string inputQueueAddress)
        {
            configurer.Register(c => new AmazonSqsTransport(inputQueueAddress, accessKeyId, secretAccessKey, regionEndpoint));
        }
    }
}
