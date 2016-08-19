using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.AzureStorage.Transport;
using Rebus.Messages;
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
            var sendInstant = DateTimeOffset.Now;
            var deferDate = sendInstant.AddMilliseconds(-350);
            RebusTimeMachine.FakeIt(sendInstant);
            var result = AzureStorageQueuesTransport.GetQueueVisibilityDelayOrNull(new Dictionary<string, string>
            {
                {Headers.DeferredUntil, deferDate.ToString("O")}
            });
            RebusTimeMachine.Reset();
            Assert.Null(result);

        }
        [Test]
        public async Task GetQueueVisibilityDelayOrNull_StillReturnsPositiveTimespans()
        {
            var sendInstant = DateTimeOffset.Now;
            var deferDate = sendInstant.AddMilliseconds(350);
            RebusTimeMachine.FakeIt(sendInstant);
            var result = AzureStorageQueuesTransport.GetQueueVisibilityDelayOrNull(new Dictionary<string, string>
            {
                {Headers.DeferredUntil, deferDate.ToString("O")}
            });
            RebusTimeMachine.Reset();
            Assert.AreEqual(result, TimeSpan.FromMilliseconds(350));

        }
    }
}