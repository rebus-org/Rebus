using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestPolymorphicDispatch : FixtureBase
{
    static readonly TimeSpan BlockingWaitTimeout = TimeSpan.FromSeconds(5);
    BuiltinHandlerActivator _handlerActivator;
    IBus _bus;

    protected override void SetUp()
    {
        _handlerActivator = new BuiltinHandlerActivator();

        _bus = Configure.With(_handlerActivator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "input"))
            .Options(o =>
            {
                o.SetNumberOfWorkers(0);
                o.SetMaxParallelism(1);
            })
            .Start();

        Using(_bus);
    }

    void StartBus() => _bus.Advanced.Workers.SetNumberOfWorkers(1);

    [Test]
    public async Task ItWorksInSimpleScenario()
    {
        var events = new ConcurrentQueue<string>();
        var gotMessage = new AutoResetEvent(false);

        _handlerActivator.Handle<BaseMessage>(async msg =>
        {
            events.Enqueue($"Got {msg.GetType().Name} with {msg.Payload}");

            gotMessage.Set();
        });

        StartBus();

        await _bus.SendLocal(new SpecializationA { Payload = "a" });
        await _bus.SendLocal(new SpecializationB { Payload = "b" });

        gotMessage.WaitOrDie(BlockingWaitTimeout, "Did not get the first message");
        gotMessage.WaitOrDie(BlockingWaitTimeout, "Did not get the second message");

        Assert.That(events.ToArray(), Is.EqualTo(new[]
        {
            "Got SpecializationA with a",
            "Got SpecializationB with b",
        }));
    }

    [Test]
    public async Task CanHandleObject()
    {
        var events = new ConcurrentQueue<string>();
        var gotMessage = new AutoResetEvent(false);

        _handlerActivator.Handle<object>(async msg =>
        {
            events.Enqueue($"Got {msg.GetType().Name}");
            gotMessage.Set();
        });

        StartBus();

        await _bus.SendLocal("hej med dig");

        gotMessage.WaitOrDie(BlockingWaitTimeout);

        Assert.That(events.ToArray(), Is.EqualTo(new[]
        {
            "Got String",
        }));
    }

    [Test]
    public async Task CanHandleInterface()
    {
        var events = new ConcurrentQueue<string>();
        var gotMessage = new AutoResetEvent(false);

        _handlerActivator.Handle<IMessage>(async msg =>
        {
            events.Enqueue($"Got {msg.GetType().Name}");
            gotMessage.Set();
        });

        StartBus();

        await _bus.SendLocal(new ImplementorOfInterface());

        gotMessage.WaitOrDie(BlockingWaitTimeout);

        Assert.That(events.ToArray(), Is.EqualTo(new[]
        {
            "Got ImplementorOfInterface",
        }));
    }

    abstract class BaseMessage
    {
        public string Payload { get; set; }
    }

    class SpecializationA : BaseMessage { }
    class SpecializationB : BaseMessage { }

    interface IMessage { }

    class ImplementorOfInterface : IMessage { }

    [Test]
    public async Task WorksWithHandlerPipelineToo()
    {
        var events = new ConcurrentQueue<string>();

        _handlerActivator
            .Register(() => new Handler1(events))
            .Register(() => new Handler2(events));

        StartBus();

        await _bus.SendLocal(new SomeMessage());

        await events.WaitUntil(q => q.Count == 2);

        Console.WriteLine($@"Got these events:

{string.Join(Environment.NewLine, events)}
");

        Assert.That(events.ToArray(), Is.EqualTo(new[]
        {
            "Handled by Handler1",
            "Handled by Handler2"
        }));
    }

    public interface ISomeInterface { }

    public class SomeMessage : ISomeInterface { }

    public class Handler1 : IHandleMessages<SomeMessage>
    {
        readonly ConcurrentQueue<string> _events;

        public Handler1(ConcurrentQueue<string> events) => _events = events;

        public async Task Handle(SomeMessage message) => _events.Enqueue("Handled by Handler1");
    }
    public class Handler2 : IHandleMessages<ISomeInterface>
    {
        readonly ConcurrentQueue<string> _events;

        public Handler2(ConcurrentQueue<string> events) => _events = events;

        public async Task Handle(ISomeInterface message) => _events.Enqueue("Handled by Handler2");
    }
}