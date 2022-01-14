using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;
using Rebus.Tests.Integration.ManyMessages;

namespace Rebus.Tests.Transport.InMem;

[TestFixture]
public class InMemTestManyMessages : TestManyMessages<InMemoryBusFactory> { }