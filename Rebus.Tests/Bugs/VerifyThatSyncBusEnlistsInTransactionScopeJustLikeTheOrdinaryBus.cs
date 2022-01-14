using System.Text;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleStringLiteral
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description("This is just to be absolutely sure that that is the case")]
public class VerifyThatSyncBusEnlistsInTransactionScopeJustLikeTheOrdinaryBus : FixtureBase
{
    [Test]
    public void ItWorks_Complete()
    {
        const string queueName = "destination-queue";
            
        var network = new InMemNetwork();
        network.CreateQueue(queueName);

        RunTest(network, destinationAddress: queueName, complete: true);

        var transportMessage = network.GetNextOrNull(queueName)?.ToTransportMessage();

        Assert.That(transportMessage, Is.Not.Null);
        Assert.That(Encoding.UTF8.GetString(transportMessage.Body), Is.EqualTo(@"""HEJ MED DIG"""));
    }

    [Test]
    public void ItWorks_DoNotComplete()
    {
        const string queueName = "destination-queue";
            
        var network = new InMemNetwork();
        network.CreateQueue(queueName);

        RunTest(network, destinationAddress: queueName, complete: false);

        var transportMessage = network.GetNextOrNull(queueName)?.ToTransportMessage();

        Assert.That(transportMessage, Is.Null);
    }

    static void RunTest(InMemNetwork network, string destinationAddress, bool complete)
    {
        using (var activator = new BuiltinHandlerActivator())
        {
            var bus = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(network, "test-queue"))
                .Routing(t => t.TypeBased().Map<string>(destinationAddress))
                .Start();

            using (var scope = new RebusTransactionScope())
            {
                bus.Advanced.SyncBus.Send("HEJ MED DIG");

                if (complete)
                {
                    scope.Complete();
                }
            }
        }
    }
}