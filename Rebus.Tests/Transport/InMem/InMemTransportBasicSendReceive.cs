using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.Tests.Transport.InMem;

[TestFixture]
public class InMemTransportBasicSendReceive : BasicSendReceive<InMemTransportFactory> { }