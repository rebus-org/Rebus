using System;

namespace Rebus.Tests.Contracts.Transports;

public interface ITransportInspectorFactory : IDisposable
{
    TransportAndInspector Create(string address);
}