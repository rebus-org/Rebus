using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Persistence.Throwing;
using Rebus.Threading;
using Rebus.Timeouts;
using Rebus.Transport;

namespace Rebus.Pipeline.Receive;

/// <summary>
/// Incoming step that checks for the presence of the <see cref="Headers.DeferredUntil"/> header, using a
/// <see cref="ITimeoutManager"/> to handle the deferral if necessary.
/// </summary>
[StepDocumentation(@"If the incoming message should not be handled now, this step saves the message until it is time to deliver the message.

This is done by checking if the incoming message has a '" + Headers.DeferredUntil + @"' header with a desired time to be delivered.")]
public class HandleDeferredMessagesStep : IIncomingStep, IDisposable, IInitializable
{
    const string DueMessagesSenderTaskName = "DueMessagesSender";

    readonly ITimeoutManager _timeoutManager;
    readonly ITransport _transport;
    readonly Options _options;
    readonly IAsyncTask _dueMessagesSenderBackgroundTask;
    readonly ILog _log;

    bool _disposed;

    /// <summary>
    /// Constructs the step, using the specified <see cref="ITimeoutManager"/> to defer relevant messages
    /// and the specified <see cref="ITransport"/> to deliver messages when they're due.
    /// </summary>
    public HandleDeferredMessagesStep(ITimeoutManager timeoutManager, ITransport transport, Options options, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        if (asyncTaskFactory == null) throw new ArgumentNullException(nameof(asyncTaskFactory));

        _timeoutManager = timeoutManager ?? throw new ArgumentNullException(nameof(timeoutManager));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _log = rebusLoggerFactory.GetLogger<HandleDeferredMessagesStep>();

        _dueMessagesSenderBackgroundTask = asyncTaskFactory.Create(DueMessagesSenderTaskName, TimerElapsed, intervalSeconds: (int)options.DueTimeoutsPollInterval.TotalSeconds);
    }

    /// <summary>
    /// Initialized the step (starts the <see cref="DueMessagesSenderTaskName"/> background task if using the internal timeout manager)
    /// </summary>
    public void Initialize()
    {
        // if the step has already been disposed, it is because it has been removed from the pipeline.... we might as well avoid doing stuff here, so let's just ignore the call
        if (_disposed) return;

        if (UsingExternalTimeoutManager)
        {
            _log.Info("Using external timeout manager with this address: {queueName}", _options.ExternalTimeoutManagerAddressOrNull);
            return;
        }

        // ATM this is the best way to detect that the timeout manager is disabled and we therefore should not start the background task that polls for due messages
        if (_timeoutManager is DisabledTimeoutManager)
        {
            return;
        }

        _dueMessagesSenderBackgroundTask.Start();
    }

    bool UsingExternalTimeoutManager => !string.IsNullOrWhiteSpace(_options.ExternalTimeoutManagerAddressOrNull);

    async Task TimerElapsed()
    {
        using var result = await _timeoutManager.GetDueMessages();

        foreach (var dueMessage in result)
        {
            var transportMessage = dueMessage.ToTransportMessage();
            var returnAddress = transportMessage.Headers[Headers.DeferredRecipient];

            _log.Debug("Sending due message {messageLabel} to {queueName}",
                transportMessage.GetMessageLabel(),
                returnAddress);

            using (var scope = new RebusTransactionScope())
            {
                await _transport.Send(returnAddress, transportMessage, scope.TransactionContext);
                await scope.CompleteAsync();
            }

            await dueMessage.MarkAsCompleted();
        }

        await result.Complete();
    }

    /// <summary>
    /// Checks to see if the incoming message has the <see cref="Headers.DeferredUntil"/> header. If that is the case, the message is either stored for later delivery
    /// or forwarded to the configured external timeout manager. If not, the message will be passed on down the pipeline.
    /// </summary>
    public async Task Process(IncomingStepContext context, Func<Task> next)
    {
        var transportMessage = context.Load<TransportMessage>();

        var headers = transportMessage.Headers;

        if (!headers.TryGetValue(Headers.DeferredUntil, out var deferredUntil))
        {
            await next();
            //return;don't return here! for some reason it is faster to have an "else"
        }
        else
        {
            if (!headers.ContainsKey(Headers.DeferredRecipient))
            {
                throw new RebusApplicationException(
                    $"Received message {headers[Headers.MessageId]} with the '{Headers.DeferredUntil}' header" +
                    $" set to '{headers[Headers.DeferredUntil]}', but the message had no" +
                    $" '{Headers.DeferredRecipient}' header!");
            }

            if (UsingExternalTimeoutManager)
            {
                var transactionContext = context.Load<ITransactionContext>();

                await ForwardMessageToExternalTimeoutManager(transportMessage, transactionContext);
            }
            else
            {
                await StoreMessageUntilDue(deferredUntil, headers, transportMessage);
            }
        }
    }

    async Task ForwardMessageToExternalTimeoutManager(TransportMessage transportMessage, ITransactionContext transactionContext)
    {
        var timeoutManagerAddress = _options.ExternalTimeoutManagerAddressOrNull;

        _log.Debug("Forwarding deferred message {messageLabel} to external timeout manager '{queueName}'",
            transportMessage.GetMessageLabel(), timeoutManagerAddress);

        await _transport.Send(timeoutManagerAddress, transportMessage, transactionContext);
    }

    async Task StoreMessageUntilDue(string deferredUntil, Dictionary<string, string> headers, TransportMessage transportMessage)
    {
        var approximateDueTime = GetTimeToBeDelivered(deferredUntil);

        _log.Debug("Deferring message {messageLabel} until {dueTime}", transportMessage.GetMessageLabel(), approximateDueTime);

        headers.Remove(Headers.DeferredUntil);

        await _timeoutManager.Defer(approximateDueTime, headers, transportMessage.Body);
    }

    static DateTimeOffset GetTimeToBeDelivered(string deferredUntil)
    {
        try
        {
            return deferredUntil.ToDateTimeOffset();
        }
        catch (Exception exception)
        {
            throw new FormatException($"Could not parse the '{Headers.DeferredUntil}' header value", exception);
        }
    }

    /// <summary>
    /// Last-resort disposal of the due messages background task
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _dueMessagesSenderBackgroundTask.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }
}