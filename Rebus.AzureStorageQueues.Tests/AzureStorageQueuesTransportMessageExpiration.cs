using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureStorageQueues.Tests
{
    [TestFixture]
    public class AzureStorageQueuesTransportMessageExpiration : MessageExpiration<AzureStorageQueuesTransportFactory> { }
}