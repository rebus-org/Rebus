using System;
using System.Collections.Concurrent;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
// ReSharper disable ConvertToUsingDeclaration
#pragma warning disable IDE0063
#pragma warning disable IDE0063

namespace Rebus.Tests.Examples;

[TestFixture]
public class ShowHowToUseRebusLifetimeEvents : FixtureBase
{
    [Test]
    public void ThisIsHowToDoit_NewStyle()
    {
        var eventlog = new ConcurrentQueue<string>();

        void Enqueue(string text)
        {
            eventlog.Enqueue(text);
            Console.WriteLine(text);
        }

        using (var activator = new BuiltinHandlerActivator())
        {
            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
                .Options(o =>
                {
                    o.Events(e =>
                    {
                        e.BusStarting += () => Enqueue("1. BusStarting");
                        e.BusStarted += () => Enqueue("2. BusStarted");
                        e.BusDisposing += () => Enqueue("3. BusDisposing");
                        e.WorkersStopped += () => Enqueue("4. WorkersStopped");
                        e.BusDisposed += () => Enqueue("5. BusDisposed");
                    });
                })
                .Start();
        }

        Assert.That(eventlog, Is.EqualTo(new[]
        {
            "1. BusStarting",
            "2. BusStarted",
            "3. BusDisposing",
            "4. WorkersStopped",
            "5. BusDisposed",
        }));
    }

    [Test]
    [Description("BusLifetimeEvents was only accessible this way previously")]
    public void ThisIsHowToDoit_OldStyle()
    {
        var eventlog = new ConcurrentQueue<string>();

        void Enqueue(string text)
        {
            eventlog.Enqueue(text);
            Console.WriteLine(text);
        }

        using (var activator = new BuiltinHandlerActivator())
        {
            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new(), "whatever"))
                .Options(o =>
                {
                    o.Decorate(c =>
                    {
                        var events = c.Get<BusLifetimeEvents>();

                        events.BusStarting += () => Enqueue("1. BusStarting");
                        events.BusStarted += () => Enqueue("2. BusStarted");
                        events.BusDisposing += () => Enqueue("3. BusDisposing");
                        events.WorkersStopped += () => Enqueue("4. WorkersStopped");
                        events.BusDisposed += () => Enqueue("5. BusDisposed");

                        return events;
                    });
                })
                .Start();
        }

        Assert.That(eventlog, Is.EqualTo(new[]
        {
            "1. BusStarting",
            "2. BusStarted",
            "3. BusDisposing",
            "4. WorkersStopped",
            "5. BusDisposed",
        }));
    }
}