using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleStringLiteral
#pragma warning disable 1998

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class PolymorphicDispatchAndSecondLevelRetries : FixtureBase
    {
        [Test]
        public async Task ItWorks()
        {
            var gotTheFailedMessage = new ManualResetEvent(initialState: false);

            var activator = Using(new BuiltinHandlerActivator());

            activator.Handle<string>(async str => throw new ApplicationException("🥓"));

            activator.Handle<IFailed<object>>(async failed => gotTheFailedMessage.Set());

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                .Options(o => o.SimpleRetryStrategy(secondLevelRetriesEnabled: true, maxDeliveryAttempts: 1))
                .Start();

            await activator.Bus.SendLocal("come on");

            gotTheFailedMessage.WaitOrDie(
                timeout: TimeSpan.FromSeconds(5),
                errorMessage: "If the reset event was not signaled within 5 s, it probably means that the IFailed<string> was not dispatched to our IFailed<object> handler"
            );
        }
    }
}