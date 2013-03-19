using System;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class CanDoManualRoutingWhenIncludingTheHostname : RebusBusMsmqIntegrationTestBase
    {
        const string InputQueueName = "test.manual.routing.with.hostname";

        [Test, Description("Reported as a bug some time, but it seems like it was a problem in the user's end. Keeping the test though, because it's nice :)")]
        public void CanDoIt()
        {
            var resetEvent = new ManualResetEvent(false);
            var stringMessageWasReceived = false;
            var activator = new HandlerActivatorForTesting()
                .Handle<string>(str =>
                    {
                        stringMessageWasReceived = true;
                        resetEvent.Set();
                    });

            var bus = CreateBus(InputQueueName, activator).Start();
            bus.Advanced.Routing.Send(InputQueueName + "@" + Environment.MachineName, "wolla my friend!");

            var timeout = TimeSpan.FromSeconds(5);
            if (!resetEvent.WaitOne(timeout))
            {
                Assert.Fail("Did not received the message withing timeout of {0}", timeout);
            }

            stringMessageWasReceived.ShouldBe(true);
        }
    }
}