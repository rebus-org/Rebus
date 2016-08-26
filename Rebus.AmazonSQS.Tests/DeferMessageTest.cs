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
    [TestFixture, Category(Category.AmazonSqs)]
    public class DeferMessageTest : SqsFixtureBase
    {
        BuiltinHandlerActivator _activator;
        RebusConfigurer _configurer;

        protected override void SetUp()
        {
            var connectionInfo = AmazonSqsTransportFactory.ConnectionInfo;

            var accessKeyId = connectionInfo.AccessKeyId;
            var secretAccessKey = connectionInfo.SecretAccessKey;
            var amazonSqsConfig = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(AmazonSqsTransportFactory.ConnectionInfo.RegionEndpoint)
            };

            var queueName = TestConfig.QueueName("defertest");

            AmazonSqsManyMessagesTransportFactory.PurgeQueue(queueName);

            _activator = Using(new BuiltinHandlerActivator());

            _configurer = Configure.With(_activator)
                .Transport(t => t.UseAmazonSqs(accessKeyId, secretAccessKey, amazonSqsConfig, queueName))
                .Options(o => o.LogPipeline());
        }

        [Test]
        public async Task CanDeferMessage()
        {
            var gotTheMessage = new ManualResetEvent(false);

            var receiveTime = DateTime.MaxValue;

            _activator.Handle<string>(async str =>
            {
                receiveTime = DateTime.UtcNow;
                gotTheMessage.Set();
            });

            var bus = _configurer.Start();

            await bus.Defer(TimeSpan.FromSeconds(10), "hej med dig!");

            gotTheMessage.WaitOrDie(TimeSpan.FromSeconds(20));

            var now = DateTime.UtcNow;
            var elapsed = now - receiveTime;

            Assert.That(elapsed, Is.GreaterThan(TimeSpan.FromSeconds(8)));
        }
    }
}