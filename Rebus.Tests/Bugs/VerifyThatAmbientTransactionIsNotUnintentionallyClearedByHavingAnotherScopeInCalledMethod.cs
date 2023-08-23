using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998

namespace Rebus.Tests.Bugs;

[TestFixture]
public class VerifyThatAmbientTransactionIsNotUnintentionallyClearedByHavingAnotherScopeInCalledMethod : FixtureBase
{
    [Test]
    public async Task CanReturnReplyTask()
    {
        var network = new InMemNetwork();

        IBus CreateBus(string queueName, Action<TypeBasedRouterConfigurationExtensions.TypeBasedRouterConfigurationBuilder> routing = null, Action<BuiltinHandlerActivator> handlers = null)
        {
            var activator = new BuiltinHandlerActivator();
            handlers?.Invoke(activator);
            return Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, queueName))
                .Routing(r => routing?.Invoke(r.TypeBased()))
                .Start();
        }

        using var successfullyReplied = new ManualResetEventSlim(initialState: false);
        using var replySuccessfullyReceived = new ManualResetEventSlim(initialState: false);

        using var sender = CreateBus("sender", routing: r => r.Map<Request>("receiver"), handlers: a => a.Handle<Reply>(async _ => replySuccessfullyReceived.Set()));
        using var receiver = CreateBus("receiver", handlers: a => a.Handle<Request>(async (bus, request) =>
        {
            await PublishMessageAsync(bus);
            await bus.Reply(new Reply());
            successfullyReplied.Set();
        }));

        async Task PublishMessageAsync(IBus bus)
        {
            using var scope = new RebusTransactionScope();
            await bus.Publish(new Event());
            await scope.CompleteAsync();
        }

        await sender.Send(new Request());

        if (!successfullyReplied.Wait(TimeSpan.FromSeconds(2)))
        {
            throw new AssertionException("Reply was not successfully sent");
        }
        if (!replySuccessfullyReceived.Wait(TimeSpan.FromSeconds(2)))
        {
            throw new AssertionException("Did not receibe reply within 2 seconds");
        }
    }

    record Event;

    record Request;

    record Reply;

}