using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports;

public abstract class TransportInspectorTest<TTransportInspectorFactory> : FixtureBase where TTransportInspectorFactory : ITransportInspectorFactory, new()
{
    TTransportInspectorFactory _factory;

    ITransport _transport;
    ITransportInspector _transportInspector;

    protected override void SetUp()
    {
        _factory = new TTransportInspectorFactory();

        Using(_factory);

        var stuff = _factory.Create("testa");

        _transport = stuff.Transport;
        _transportInspector = stuff.TransportInspector;

        (_transport as IInitializable)?.Initialize();
        (_transportInspector as IInitializable)?.Initialize();
    }

    [Test]
    public async Task InitialCountIsZero()
    {
        var info = await _transportInspector.GetProperties(CancellationToken.None);
        var count = Convert.ToInt32(info[TransportInspectorPropertyKeys.QueueLength]);

        Assert.That(count, Is.EqualTo(0));
    }
}