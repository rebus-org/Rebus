using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Activation;

public abstract class RealContainerTests<TActivationContext> : FixtureBase where TActivationContext : IActivationContext, new()
{
    TActivationContext _activationCtx;

    protected override void SetUp()
    {
        _activationCtx = new TActivationContext();
    }

    [Test]
    public void BusIsDisposedAfterUse()
    {
        var wasDisposed = false;

        _activationCtx
            .CreateBus(configurer => configurer
                .Transport(t =>
                {
                    t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter");
                })
                .Options(o =>
                {
                    o.Register(c => new DisposableCallbacker(() => wasDisposed = true));

                    o.Decorate(c =>
                    {
                        // force this into resolution context to have it disposed
                        c.Get<DisposableCallbacker>();
                        return c.Get<Options>();
                    });
                }), out var container);

        container.ResolveBus();

        container.Dispose();

        Assert.That(wasDisposed, Is.True, "The registered DisposableCallbacker was not disposed, which indicates that the bus probably wasn't either");
    }

    class DisposableCallbacker : IDisposable
    {
        readonly Action _disposed;

        public DisposableCallbacker(Action disposed) => _disposed = disposed;

        public void Dispose() => _disposed();
    }

    [Test]
    public async Task CanInjectSyncBus()
    {
        HandlerThatGetsSyncBusInjected.SyncBusWasInjected = false;

        var bus = _activationCtx.CreateBus(
            handlers => handlers.Register<HandlerThatGetsSyncBusInjected>(),
            configure => configure.Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "hvassåbimse")));

        Using(bus);

        await bus.SendLocal("hej");

        await Task.Delay(500);

        Assert.That(HandlerThatGetsSyncBusInjected.SyncBusWasInjected, Is.True,
            "HandlerThatGetsSyncBusInjected did not get invoked properly with an injected ISyncBus");
    }

    [Test]
    public async Task CanInjectMessageContext()
    {
        HandlerThatGetsMessageContextInjected.MessageContextWasInjected = false;

        var bus = _activationCtx.CreateBus(
            handlers => handlers.Register<HandlerThatGetsMessageContextInjected>(),
            configure => configure.Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "hvassåbimse")));

        Using(bus);

        await bus.SendLocal("hej");

        await Task.Delay(500);

        Assert.That(HandlerThatGetsMessageContextInjected.MessageContextWasInjected, Is.True,
            "HandlerThatGetsMessageContextInjected did not get invoked properly with an injected IMessageContext");
    }

    class HandlerThatGetsMessageContextInjected : IHandleMessages<string>
    {
        public static bool MessageContextWasInjected;

        readonly IMessageContext _messageContext;

        public HandlerThatGetsMessageContextInjected(IMessageContext messageContext)
        {
            _messageContext = messageContext;
        }

        public async Task Handle(string message)
        {
            if (_messageContext != null)
            {
                MessageContextWasInjected = true;
            }
        }
    }

    class HandlerThatGetsSyncBusInjected : IHandleMessages<string>
    {
        public static bool SyncBusWasInjected;

        readonly ISyncBus _syncBus;

        public HandlerThatGetsSyncBusInjected(ISyncBus syncBus)
        {
            _syncBus = syncBus;
        }

        public async Task Handle(string message)
        {
            if (_syncBus != null)
            {
                SyncBusWasInjected = true;
            }
        }
    }
}