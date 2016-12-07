using System;
using System.Threading;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    /*

    To avoid accidentally sending the received IFailed<YourMessage>
    somewhere when you are using 2nd level retries, we want to throw
    a nice explanation if that happens.

    */

    public class TestCannotSendFailedMessageWrapper : FixtureBase
    {
        readonly BuiltinHandlerActivator _activator;

        public TestCannotSendFailedMessageWrapper()
        {
            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Logging(l => l.None())
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "FAIL"))
                .Options(o =>
                {
                    o.SimpleRetryStrategy(secondLevelRetriesEnabled: true, maxDeliveryAttempts: 1);
                })
                .Start();
        }

        [Fact]
        public void CannotSendFailedMessageWrapper()
        {
            var couldNotSendMessage = new ManualResetEvent(false);

            Exception caughtException = null;

            _activator.Handle<string>(async str =>
            {
                throw new ArithmeticException();
            });

            _activator.Handle<IFailed<string>>(async (bus, context, failed) =>
            {
                // "failed" is an IFailed<string> in here - let's pretend that
                // we accidentally Defer that one instead of "failed.Message"
                // as we should

                try
                {
                    await bus.Defer(TimeSpan.FromSeconds(1), failed);
                }
                catch(Exception exception)
                {
                    // just what we wanted! :)
                    caughtException = exception;
                    couldNotSendMessage.Set();
                }
            });

            _activator.Bus.SendLocal("HOOLOOBOOLOO").Wait();

            couldNotSendMessage.WaitOrDie(TimeSpan.FromSeconds(5));

            Console.WriteLine($@"This is the exception that we caught:

{caughtException}

You're welcome.");
        }
    }
}