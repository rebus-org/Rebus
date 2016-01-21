using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.AmazonSQS.Config;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Tests.Extensions;

#pragma warning disable 1998

namespace Rebus.AmazonSQS.Tests
{
    [TestFixture]
    public class DeferMessageTest : SqsFixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            var connectionInfo = AmazonSqsTransportFactory.ConnectionInfo;

            var accessKeyId = connectionInfo.AccessKeyId;
            var secretAccessKey = connectionInfo.SecretAccessKey;
            var amazonSqsConfig = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(AmazonSqsTransportFactory.ConnectionInfo.RegionEndpoint)
            };

            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Transport(t => t.UseAmazonSqs(accessKeyId, secretAccessKey, amazonSqsConfig, TestConfig.QueueName("defertest")))
                .Options(o => o.LogPipeline())
                .Start();
        }

        [Test]
        public async Task CanDeferMessage()
        {
            var stopwatch = Stopwatch.StartNew();
            var gotTheMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async str =>
            {
                stopwatch.Stop();
                gotTheMessage.Set();
            });

            await _activator.Bus.Defer(TimeSpan.FromSeconds(4), "hej med dig!");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(8));

            Assert.That(stopwatch.Elapsed, Is.GreaterThan(TimeSpan.FromSeconds(4)));
        }
    }
}