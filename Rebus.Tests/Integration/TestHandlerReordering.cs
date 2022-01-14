using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Handlers.Reordering;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestHandlerReordering : FixtureBase
{
    [Test]
    public async Task CanReorderHandlers()
    {
        var events = new ConcurrentQueue<string>();
        var activator = new FakeHandlerActivator(new IHandleMessages[]
        {
            new ThirdHandler(events), 
            new SecondHandler(events), 
            new FirstHandler(events), 
        });

        var bus = Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "handler-reordering"))
            .Options(o =>
            {
                o.SpecifyOrderOfHandlers()
                    .First<FirstHandler>()
                    .Then<SecondHandler>();
            })
            .Start();

        Using(bus);

        await bus.SendLocal("hej med dig min ven");

        await events.WaitUntil(e => e.Count == 3);

        Assert.That(events.ToArray(), Is.EqualTo(new[] { "FirstHandler", "SecondHandler", "ThirdHandler" }));
    }

    class FirstHandler : IHandleMessages<string>
    {
        readonly ConcurrentQueue<string> _events;

        public FirstHandler(ConcurrentQueue<string> events)
        {
            _events = events;
        }

        public async Task Handle(string message)
        {
            _events.Enqueue("FirstHandler");
        }
    }

    class SecondHandler : IHandleMessages<string>
    {
        readonly ConcurrentQueue<string> _events;

        public SecondHandler(ConcurrentQueue<string> events)
        {
            _events = events;
        }

        public async Task Handle(string message)
        {
            _events.Enqueue("SecondHandler");
        }
    }

    class ThirdHandler : IHandleMessages<string>
    {
        readonly ConcurrentQueue<string> _events;

        public ThirdHandler(ConcurrentQueue<string> events)
        {
            _events = events;
        }

        public async Task Handle(string message)
        {
            _events.Enqueue("ThirdHandler");
        }
    }

    class FakeHandlerActivator : IHandlerActivator
    {
        readonly IEnumerable<IHandleMessages> _handlers;

        public FakeHandlerActivator(IEnumerable<IHandleMessages> handlers)
        {
            _handlers = handlers;
        }

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            return _handlers.OfType<IHandleMessages<TMessage>>();
        }
    }
}