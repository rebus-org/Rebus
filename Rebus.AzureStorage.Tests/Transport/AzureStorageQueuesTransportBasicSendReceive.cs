using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.AzureStorage.Transport;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Tests.Contracts.Transports;
using Rebus.Time;

namespace Rebus.AzureStorage.Tests.Transport
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStorageQueuesTransportBasicSendReceive : BasicSendReceive<AzureStorageQueuesTransportFactory>
    {
        [Test]
        public async Task GetQueueVisibilityDelayOrNull_NeverReturnsNegativeTimespans()
        {
            RebusTimeMachine.FakeIt(DateTimeOffset.Parse("2016-06-25T12:20:52.6038001-04:00"));
            var result = AzureStorageQueuesTransport.GetQueueVisibilityDelayOrNull(new Dictionary<string, string>
            {
                {Headers.DeferredUntil, "2016-06-25T12:00:00.6038001-04:00"}
            });
            RebusTimeMachine.Reset();
            Assert.Null(result);

        }
        [Test]
        public async Task GetQueueVisibilityDelayOrNull_StillReturnsPositiveTimespans()
        {
            RebusTimeMachine.FakeIt(DateTimeOffset.Parse("2016-06-25T12:00:00.6038001-04:00"));
            var result = AzureStorageQueuesTransport.GetQueueVisibilityDelayOrNull(new Dictionary<string, string>
            {
                {Headers.DeferredUntil, "2016-06-25T12:20:52.6038001-04:00"}
            });
            RebusTimeMachine.Reset();
            Assert.Greater(result, TimeSpan.Zero);

        }
    }
}