using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Bugs
{
    // When passing custom headers, there was an error whereby the passed-in headers dictionary would be passed by reference through the pipeline, causing e.g. rbs2-msg-id to be added to it.
    public class CustomHeadersAreCloned : FixtureBase
    {
        [Fact]
        public async Task ItHasBeenFixed()
        {
            var activator = new BuiltinHandlerActivator();

            Using(activator);

            var receivedMessageIds = new ConcurrentBag<string>();

            activator.Handle<string>(async (_, context, message) =>
            {
                receivedMessageIds.Add(context.TransportMessage.Headers[Headers.MessageId]);
            });

            var bus = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "buggerino"))
                .Start();

            var customHeaders = new Dictionary<string, string>
            {
                {"custom-header", "woohoo"}
            };

            const string repeatedMessage = "hej med dig";

            await bus.SendLocal(repeatedMessage, customHeaders);
            await bus.SendLocal("hej igen med", customHeaders);
            await bus.SendLocal(repeatedMessage, customHeaders);

            await Task.Delay(TimeSpan.FromSeconds(1));

            Assert.Equal(3, receivedMessageIds.Distinct().Count());
        }
    }
}