using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Auditing
{
    [TestFixture]
    public class TestSentTimeHeader : FixtureBase
    {
        [Test]
        public async Task VerifyThatExplicitlySetSentTimeHeaderDoesNotGetOverwritten()
        {
            var sentTimeTaskSource = new TaskCompletionSource<string>();
            var activator = Using(new BuiltinHandlerActivator());

            // intentionally set this a little bit in the past
            var fakeSentTime = DateTimeOffset.Now.AddMinutes(-2);

            activator.Handle<string>(async (bus, context, msg) =>
            {
                try
                {
                    sentTimeTaskSource.SetResult(context.Headers.GetValue(Headers.SentTime));
                }
                catch (Exception exception)
                {
                    sentTimeTaskSource.SetException(exception);
                }
            });

            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "who cares"))
                .Start();

            var headers = new Dictionary<string, string>
            {
                [Headers.SentTime] = fakeSentTime.ToIso8601DateTimeOffset()
            };

            await activator.Bus.SendLocal("HEJ", headers);

            var sentTime = await sentTimeTaskSource.Task;

            Assert.That(sentTime, Is.EqualTo(fakeSentTime.ToIso8601DateTimeOffset()));
        }
    }
}