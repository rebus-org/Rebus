using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Bugs;

[TestFixture]
public class BeAbsolutelySureThatTheBuiltinHandlerActivatorWorks : FixtureBase
{
    [Test]
    public async Task MultipleHandlerCallbacks()
    {
        var activator = Using(new BuiltinHandlerActivator());

        var gotIt1 = Using(new ManualResetEvent(initialState: false));
        var gotIt2 = Using(new ManualResetEvent(initialState: false));
        var gotIt3 = Using(new ManualResetEvent(initialState: false));

        activator.Handle<PopularMessageEverybodyWantsIt>(async _ => gotIt1.Set());
        activator.Handle<PopularMessageEverybodyWantsIt>(async _ => gotIt2.Set());
        activator.Handle<PopularMessageEverybodyWantsIt>(async _ => gotIt3.Set());

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "some-queue"))
            .Start();

        await activator.Bus.SendLocal(new PopularMessageEverybodyWantsIt());

        await Task.WhenAll(
            gotIt1.WaitAsync(),
            gotIt2.WaitAsync(),
            gotIt3.WaitAsync());
    }

    [Test]
    public async Task PolymorphicCallback()
    {
        var activator = Using(new BuiltinHandlerActivator());

        var derivedMessageCallbackCalled = Using(new ManualResetEvent(initialState: false));
        var baseMessageCallbackCalled = Using(new ManualResetEvent(initialState: false));

        activator.Handle<PopularMessageEverybodyWantsIt>(async _ => derivedMessageCallbackCalled.Set());
        activator.Handle<BaseMessage>(async _ => baseMessageCallbackCalled.Set());

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "some-queue"))
            .Start();

        await activator.Bus.SendLocal(new PopularMessageEverybodyWantsIt());

        await Task.WhenAll(
            derivedMessageCallbackCalled.WaitAsync(),
            baseMessageCallbackCalled.WaitAsync()
        );
    }


    class BaseMessage { }
    class PopularMessageEverybodyWantsIt : BaseMessage { }
}