using NUnit.Framework;
using Rebus.Tests.Contracts.Transports;

namespace Rebus.Tests.Transport.InMem;

[TestFixture]
public class InMemTransportMessageExpiration : MessageExpiration<InMemTransportFactory> { }