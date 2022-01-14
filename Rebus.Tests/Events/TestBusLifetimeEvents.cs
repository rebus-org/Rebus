using System;
using System.Collections.Concurrent;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Events;

[TestFixture]
public class TestBusLifetimeEvents : FixtureBase
{
    [Test]
    public void RecordsTheEventsAsExpected()
    {
        const string body = "hej med dig!!";
        const string queueName = "lifetime-events";

        var recordedEvents = new ConcurrentQueue<string>();
        var network = new InMemNetwork();

        using (var bus = StartBus(network, queueName, recordedEvents))
        {
            Thread.Sleep(500);

            bus.SendLocal(body).Wait();

            Thread.Sleep(500);
        }

        Console.WriteLine(string.Join(Environment.NewLine, recordedEvents));
    }

    static IBus StartBus(InMemNetwork network, string queueName, ConcurrentQueue<string> recordedEvents)
    {
        var activator = new BuiltinHandlerActivator();

        activator.Handle(async (string message) =>
        {
            recordedEvents.Enqueue($"GOT MESSAGE: {message}");
        });

        return Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, queueName))
            .Options(o =>
            {
                o.Decorate(c =>
                {
                    var events = c.Get<BusLifetimeEvents>();

                    events.BusStarting += () => recordedEvents.Enqueue("Bus starting");
                    events.BusStarted += () => recordedEvents.Enqueue("Bus started");
                    events.BusDisposing += () => recordedEvents.Enqueue("Bus disposing");
                    events.BusDisposed += () => recordedEvents.Enqueue("Bus disposed");

                    return events;
                });
            })
            .Start();
    }
}