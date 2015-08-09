using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureStorageQueues.Tests
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStorageQueuesTransportBasicSendReceive : BasicSendReceive<AzureStorageQueuesTransportFactory> { }
}