using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable AccessToDisposedClosure
#pragma warning disable CS1998

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("Thought that there was an issue with the rbs2-defer-recipient header being overwritten. There wasn't.")]
public class CanSpecifyDeferRecipientWithHeaderWhenDeferring : FixtureBase
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task CanSpecifyRecipientOfDeferredMessageWithMessageHeader(bool useExplicitRouting)
    {
        var network = new InMemNetwork();

        using var receiver = new BuiltinHandlerActivator();
        using var gotTheMessage = new ManualResetEvent(initialState: false);

        receiver.Handle<string>(async _ => gotTheMessage.Set());

        Configure.With(receiver)
            .Transport(t => t.UseInMemoryTransport(network, "receiver-queue"))
            .Timeouts(t => t.StoreInMemory())
            .Start();

        using var sender = new BuiltinHandlerActivator();

        Configure.With(sender)
            .Transport(t => t.UseInMemoryTransport(network, "sender-queue"))
            .Timeouts(t => t.StoreInMemory())
            .Routing(r =>
            {
                var destinationQueue = useExplicitRouting
                    ? "this-queue-does-not-exist, so the explicitly specified destination MUST be used, or else the message will not be received" //< will not work :)
                    : "receiver-queue";

                r.TypeBased().Map<string>(destinationQueue);
            })
            .Start();

        var headers = useExplicitRouting
            ? new Dictionary<string, string> { [Headers.DeferredRecipient] = "receiver-queue" } //< necessary when using explicit routing!
            : new Dictionary<string, string>();

        await sender.Bus.Defer(TimeSpan.FromSeconds(0.1), "HEJ MED DIG 🙂", optionalHeaders: headers);

        gotTheMessage.WaitOrDie(timeout: TimeSpan.FromSeconds(3));
    }
}