using Rebus.Tests.Contracts.Transports;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.Tests.Transport.InMem
{
    public class InMemTestManyMessages : TestManyMessages<InMemoryBusFactory> { }
}