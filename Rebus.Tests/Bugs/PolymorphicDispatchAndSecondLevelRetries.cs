using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleStringLiteral
// ReSharper disable ArgumentsStyleLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
public class PolymorphicDispatchAndSecondLevelRetries : FixtureBase
{
    [Test]
    public async Task ItWorks_Inline()
    {
        var gotTheFailedMessage = new ManualResetEvent(initialState: false);

        var activator = Using(new BuiltinHandlerActivator());

        activator.Handle<string>(async str => throw new ApplicationException("🥓"));

        activator.Handle<IFailed<object>>(async failed => gotTheFailedMessage.Set());

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
            .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true, maxDeliveryAttempts: 1))
            .Start();

        await activator.Bus.SendLocal("come on");

        gotTheFailedMessage.WaitOrDie(
            timeout: TimeSpan.FromSeconds(5),
            errorMessage: "If the reset event was not signaled within 5 s, it probably means that the IFailed<string> was not dispatched to our IFailed<object> handler"
        );
    }

    [Test]
    public async Task ItWorks_Class()
    {
        var gotTheFailedMessage = new ManualResetEvent(initialState: false);

        var activator = Using(new BuiltinHandlerActivator());

        activator.Handle<string>(async str => throw new ApplicationException("🥓"));

        activator.Register(() => new PolymorphicHandler(gotTheFailedMessage));

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
            .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true, maxDeliveryAttempts: 1))
            .Start();

        await activator.Bus.SendLocal("come on");

        gotTheFailedMessage.WaitOrDie(
            timeout: TimeSpan.FromSeconds(5),
            errorMessage: "If the reset event was not signaled within 5 s, it probably means that the IFailed<string> was not dispatched to our IFailed<object> handler"
        );
    }

    class PolymorphicHandler : IHandleMessages<IFailed<object>>
    {
        readonly ManualResetEvent _gotTheFailedMessage;

        public PolymorphicHandler(ManualResetEvent gotTheFailedMessage) => _gotTheFailedMessage = gotTheFailedMessage;

        public async Task Handle(IFailed<object> message) => _gotTheFailedMessage.Set();
    }
}