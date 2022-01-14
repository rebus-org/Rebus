using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Integration;

[TestFixture]
[Description(@"

To avoid accidentally sending the received IFailed<YourMessage>
somewhere when you are using 2nd level retries, we want to throw
a nice explanation if that happens.

")]
public class TestCannotSendFailedMessageWrapper : FixtureBase
{
    BuiltinHandlerActivator _activator;
    IBusStarter _starter;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _starter = Configure.With(_activator)
            .Logging(l => l.None())
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "FAIL"))
            .Options(o =>
            {
                o.SimpleRetryStrategy(secondLevelRetriesEnabled: true, maxDeliveryAttempts: 1);
            })
            .Create();
    }

    [Test]
    public void CannotSendFailedMessageWrapper()
    {
        var couldNotSendMessage = new ManualResetEvent(false);

        Exception caughtException = null;

        _activator.Handle<string>(async str => throw new ArithmeticException());

        _activator.Handle<IFailed<string>>(async (bus, context, failed) =>
        {
            // "failed" is an IFailed<string> in here - let's pretend that
            // we accidentally Defer that one instead of "failed.Message"
            // as we should

            try
            {
                await bus.Defer(TimeSpan.FromSeconds(1), failed);
            }
            catch (Exception exception)
            {
                // just what we wanted! :)
                caughtException = exception;
                couldNotSendMessage.Set();
            }
        });

        var buss = _starter.Start();
            
        buss.SendLocal("HOOLOOBOOLOO").Wait();

        couldNotSendMessage.WaitOrDie(TimeSpan.FromSeconds(5));

        Console.WriteLine($@"This is the exception that we caught:

{caughtException}

You're welcome.");
    }
}