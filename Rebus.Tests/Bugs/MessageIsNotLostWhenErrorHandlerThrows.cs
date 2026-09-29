using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable CS1998

namespace Rebus.Tests.Bugs;

[TestFixture]
[Description(@"Reproduces a production incident: a handler depends on a service that becomes unavailable, the 2nd level retry fails for the
same reason, and the custom error handler also depends on that service and throws. The exception escapes DefaultRetryStep before
SetResult is called, and TransactionContext.Dispose does not NACK when no result was set - so the message is neither ACKed nor NACKed.
With the in-mem transport the message is silently lost; with RabbitMQ it permanently occupies a slot in the prefetch window.")]
public class MessageIsNotLostWhenErrorHandlerThrows : FixtureBase
{
    const string QueueName = "error-handler-throws";

    [Test]
    public async Task MessageIsReturnedToQueue_WhenErrorHandlerThrows()
    {
        var network = new InMemNetwork();
        var logs = new ListLoggerFactory(outputToConsole: true);
        var activator = Using(new BuiltinHandlerActivator());
        var handlerAttempts = 0;

        activator.Handle<string>(async _ =>
        {
            Interlocked.Increment(ref handlerAttempts);
            throw new HttpRequestException("Connection refused");
        });

        activator.Handle<IFailed<string>>(async _ => throw new HttpRequestException("Connection refused (2nd level)"));

        var bus = Configure.With(activator)
            .Logging(l => l.Use(logs))
            .Transport(t => t.UseInMemoryTransport(network, QueueName))
            .Options(o =>
            {
                o.RetryStrategy(maxDeliveryAttempts: 2, secondLevelRetriesEnabled: true);
                o.Decorate<IErrorHandler>(_ => new ThrowingErrorHandler());
            })
            .Start();

        await bus.SendLocal("hello");

        await logs.WaitUntil(lines => lines.Any(l => l.Level == LogLevel.Error), timeoutSeconds: 5);

        // let the worker finish disposing the transaction context
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        Assert.That(handlerAttempts, Is.GreaterThanOrEqualTo(2), "Expected the handler to have been attempted on both deliveries");

        Assert.That(network.Count(QueueName) > 0 || handlerAttempts > 2, Is.True,
            "Expected the message to be NACKed back to the input queue (and redelivered) after the error handler threw, but it was lost: " +
            $"the input queue holds {network.Count(QueueName)} message(s) and the error queue holds {network.Count("error")}");
    }

    class ThrowingErrorHandler : IErrorHandler
    {
        public Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception) =>
            throw new HttpRequestException("Connection refused (error handler depends on the same unavailable service)");
    }
}
