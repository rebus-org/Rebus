using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureStorage.Tests.Transport
{
    [TestFixture, Category(TestCategory.Azure)]
    public class AzureStorageQueuesTransportMessageExpiration : MessageExpiration<AzureStorageQueuesTransportFactory>
    {
    }
}