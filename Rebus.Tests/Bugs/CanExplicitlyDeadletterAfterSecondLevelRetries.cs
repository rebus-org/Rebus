using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Rebus.Tests.Bugs;

[TestFixture]
public class CanExplicitlyDeadletterAfterSecondLevelRetries : FixtureBase
{
    [Test]
    [Description($"Second-level retries are invoked via an alternative path in {nameof(DefaultRetryStep)} and it is important that a manually dead-lettered message does in fact get dead-lettered there also")]
    public async Task VerifyThatItWorks()
    {
        var network = new InMemNetwork();

        using var activator = new BuiltinHandlerActivator();

        activator.Register((bus, _) => new MyMessageHandler(bus));

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "whatever"))
            .Options(o => o.RetryStrategy(secondLevelRetriesEnabled: true))
            .Start();

        await activator.Bus.SendLocal("HEJ");

        var failedMessage = await network.WaitForNextMessageFrom("error");

        Assert.That(Encoding.UTF8.GetString(failedMessage.Body), Is.EqualTo("\"HEJ\""));
    }

    class MyMessageHandler(IBus bus) : IHandleMessages<string>, IHandleMessages<IFailed<string>>
    {
        public async Task Handle(string message) => throw new ApplicationException("just fail");

        public async Task Handle(IFailed<string> message)
        {
            var failedMessageExceptions = message.Exceptions;

            await bus.Advanced.TransportMessage.Deadletter(failedMessageExceptions.First().GetFullErrorDescription());
        }
    }
}