using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureTableStorage.Tests
{
    [TestFixture]
    public class AzureTableStorageBasicSendReceive : BasicSendReceive<AzureTableStorageTransportFactory> { }
}