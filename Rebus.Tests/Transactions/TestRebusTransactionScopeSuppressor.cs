using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Transactions;

[TestFixture]
public class TestRebusTransactionScopeSuppressor : FixtureBase
{
    [Test]
    public async Task SuppressorSuppressesAsItShould()
    {
        var activator = Using(new BuiltinHandlerActivator());

        var messagesHandled = 0;

        activator.Handle<string>(async str => Interlocked.Increment(ref messagesHandled));

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bimmelimmelim"))
            .Start();

        using (new RebusTransactionScope())
        {
            using (new RebusTransactionScopeSuppressor())
            {
                await activator.Bus.SendLocal("HEJ SØTTE");
            }
        }

        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.That(messagesHandled, Is.EqualTo(1), 
            @"Expected one message to have been handled, because it was sent from a suppressed transaction scope == no scope :)");
    }    
}