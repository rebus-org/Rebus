using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Retry.PoisonQueues;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Retry.PoisonQueues;

[TestFixture]
public class TestPoisonQueueErrorHandler : FixtureBase
{
    readonly InMemNetwork _network = new InMemNetwork(true);

    PoisonQueueErrorHandler _handler;
    InMemTransport _transport;
    SimpleRetryStrategySettings _simpleRetryStrategySettings;

    protected override void SetUp()
    {
        _network.Reset();

        _simpleRetryStrategySettings = new SimpleRetryStrategySettings();
        _transport = new InMemTransport(_network, "whatever");
        _handler = new PoisonQueueErrorHandler(_simpleRetryStrategySettings, _transport, new ConsoleLoggerFactory(false));
        _handler.Initialize();
    }

    [Test]
    public async Task MovesMessageToErrorQueueAsExpected()
    {
        var message = NewMessage("known-id");
        var exception = new IOException("æi altså");

        await WithContext(async context =>
        {
            await _handler.HandlePoisonMessage(message, context, exception);
        });

        var failedMessage = _network.GetNextOrNull("error");

        Assert.That(failedMessage, Is.Not.Null);
        Assert.That(failedMessage.Headers[Headers.MessageId], Is.EqualTo("known-id"));
    }

    [Test]
    public async Task TruncatesErrorDetailsIfTheyAreTooLong()
    {
        _simpleRetryStrategySettings.ErrorDetailsHeaderMaxLength = 300;

        var message = NewMessage("known-id");
        var exception = new IOException(new string('*', 1024));
        var originalErrorDetails = exception.ToString();

        await WithContext(async context =>
        {
            await _handler.HandlePoisonMessage(message, context, exception);
        });

        var failedMessage = _network.GetNextOrNull("error");
        var truncatedErrorDetails = failedMessage.Headers[Headers.ErrorDetails];

        Console.WriteLine($@"

-------------------------------------------------------------




The error details originally looked like this:

{originalErrorDetails}

(length: {originalErrorDetails.Length})

The forwarded message contained these error details:

{truncatedErrorDetails}

(length: {truncatedErrorDetails.Length})
");

        Assert.That(truncatedErrorDetails.Length, Is.LessThanOrEqualTo(_simpleRetryStrategySettings.ErrorDetailsHeaderMaxLength));
    }

    static TransportMessage NewMessage(string messageId = null)
    {
        var headers = new Dictionary<string, string>
        {
            {Headers.MessageId, messageId ?? (Guid.NewGuid()).ToString()}
        };

        return new TransportMessage(headers, new byte[] { 1, 2, 3 });
    }

    async Task WithContext(Func<ITransactionContext, Task> action)
    {
        var context = new TransactionContext();
        await action(context);
        await context.Complete();
    }
}