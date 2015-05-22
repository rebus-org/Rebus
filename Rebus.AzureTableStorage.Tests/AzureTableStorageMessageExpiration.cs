using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.AzureTableStorage.Tests
{
    [TestFixture]
    public class AzureTableStorageMessageExpiration : MessageExpiration<AzureTableStorageTransportFactory> { }
}