using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Integration
{
    public class TestShutdownWithPendingTasks : FixtureBase
    {
        [Fact]
        public async Task DoIt()
        {
            var builtinHandlerActivator = new BuiltinHandlerActivator();
            var allDone = false;
            var gotMessage = new ManualResetEvent(false);

            builtinHandlerActivator.Handle<string>(async _ =>
            {
                gotMessage.Set();

                await Task.Delay(2000);

                allDone = true;
            });

            var bus = Configure.With(builtinHandlerActivator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "shutdown with pending tasks"))
                .Start();

            using (bus)
            {
                await bus.SendLocal("hej");

                gotMessage.WaitOrDie(TimeSpan.FromSeconds(2));

                // make bus shut down here
            }

            Assert.True(allDone, "The message was apparently not handled all the way to the end!!!");
        }
    }
}