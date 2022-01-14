using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestAsyncHandler : FixtureBase
{
    static readonly string InputQueueName = TestConfig.GetName("test.async.input");
    IBusStarter _bus;
    BuiltinHandlerActivator _handlerActivator;

    protected override void SetUp()
    {
        _handlerActivator = new BuiltinHandlerActivator();

        Using(_handlerActivator);

        _bus = Configure.With(_handlerActivator)
            .Routing(r => r.TypeBased().Map<string>(InputQueueName))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), InputQueueName))
            .Options(o => o.SetNumberOfWorkers(1))
            .Create();
    }

    [Test]
    public async Task YeahItWorks()
    {
        var events = new List<string>();
        var finishedHandled = new ManualResetEvent(false);

        _handlerActivator.Handle<string>(async str =>
        {
            await AppendEvent(events, "1");
            await AppendEvent(events, "2");
            await AppendEvent(events, "3");
            await AppendEvent(events, "4");
            finishedHandled.Set();
        });

        Console.WriteLine(string.Join(Environment.NewLine, events));

        var bus = _bus.Start();
        await bus.Send("hej med dig!");

        finishedHandled.WaitOrDie(TimeSpan.FromSeconds(10));

        Assert.That(events.Count, Is.EqualTo(4));
        Assert.That(events[0], Does.StartWith("event=1"));
        Assert.That(events[1], Does.StartWith("event=2"));
        Assert.That(events[2], Does.StartWith("event=3"));
        Assert.That(events[3], Does.StartWith("event=4"));
    }

    static async Task AppendEvent(ICollection<string> events, string eventNumber)
    {
        var text = $"event={eventNumber};thread={Thread.CurrentThread.ManagedThreadId};time={DateTime.UtcNow:mm:ss};context={AmbientTransactionContext.Current}";

        Console.WriteLine(text);

        events.Add(text);

        await Task.Delay(10);
    }
}