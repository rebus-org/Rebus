using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Retry.ErrorTracking;
using Rebus.Retry.FailFast;
using Rebus.Retry.Info;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Time;
using Rebus.Transport;
#pragma warning disable CS1998

namespace Rebus.Tests.Retry.Simple;

/// <summary>
/// Every path through <see cref="DefaultRetryStep"/> that hands a message to <see cref="IErrorHandler"/> does so outside a
/// try/catch. If the error handler throws (e.g. because it depends on the same unavailable service that made the handler
/// fail), the exception escapes the step before <see cref="ITransactionContext.SetResult"/> is called, so the message is
/// neither ACKed nor NACKed. The step must NACK in that case, so the message is returned and dead-lettering can be retried.
/// </summary>
[TestFixture]
public class TestDefaultRetryStepWhenErrorHandlerThrows
{
    const string MessageId = "message-id";

    public enum Scenario
    {
        FinalDeliveryAttemptFails,
        FailFastExceptionIsThrown,
        SecondLevelRetryFails,
        NextDeliveryModeAfterTooManyFailures,
        NativeDeliveryCountExceeded,
        MessageIdIsMissing,
    }

    [TestCase(Scenario.FinalDeliveryAttemptFails)]
    [TestCase(Scenario.FailFastExceptionIsThrown)]
    [TestCase(Scenario.SecondLevelRetryFails)]
    [TestCase(Scenario.NextDeliveryModeAfterTooManyFailures)]
    [TestCase(Scenario.NativeDeliveryCountExceeded)]
    [TestCase(Scenario.MessageIdIsMissing)]
    public async Task NacksMessage_WhenErrorHandlerThrows(Scenario scenario)
    {
        var errorHandler = new ThrowingErrorHandler();
        var transactionContext = new RecordingTransactionContext();

        var (settings, headers, next) = scenario switch
        {
            Scenario.FinalDeliveryAttemptFails => (
                new RetryStrategySettings(maxDeliveryAttempts: 1),
                Headers(),
                AlwaysThrow(() => new InvalidOperationException("handler failed"))),

            Scenario.FailFastExceptionIsThrown => (
                new RetryStrategySettings(),
                Headers(),
                AlwaysThrow(() => new FailFastException("handler failed fast"))),

            Scenario.SecondLevelRetryFails => (
                new RetryStrategySettings(maxDeliveryAttempts: 1, secondLevelRetriesEnabled: true),
                Headers(),
                AlwaysThrow(() => new InvalidOperationException("handler failed, and so did the 2nd level handler"))),

            Scenario.NextDeliveryModeAfterTooManyFailures => (
                new RetryStrategySettings(maxDeliveryAttempts: 1, errorHandlerMode: ErrorHandlerMode.NextDelivery),
                Headers(),
                Succeed()),

            Scenario.NativeDeliveryCountExceeded => (
                new RetryStrategySettings(),
                Headers((Rebus.Messages.Headers.DeliveryCount, "99")),
                Succeed()),

            Scenario.MessageIdIsMissing => (
                new RetryStrategySettings(),
                new Dictionary<string, string>(),
                Succeed()),

            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };

        var errorTracker = CreateErrorTracker(settings);

        if (scenario == Scenario.NextDeliveryModeAfterTooManyFailures)
        {
            // simulate that the previous delivery failed and was NACKed
            await errorTracker.RegisterError(MessageId, new InvalidOperationException("previous delivery failed"));
        }

        var step = CreateStep(settings, errorHandler, errorTracker, CancellationToken.None);

        await ProcessIgnoringExceptions(step, CreateContext(headers, transactionContext), next);

        Assert.That(errorHandler.Invocations, Is.EqualTo(1), "Expected the scenario to reach the error handler");
        Assert.That(transactionContext.Results, Is.EqualTo(new[] { (commit: false, ack: false) }),
            "Expected the retry step to NACK the message when the error handler throws - without a result, the message is neither ACKed nor NACKed");
    }

    [Test]
    [Description("The 1st level dispatch sets a NACK result when cancelled during shutdown - the 2nd level dispatch does not")]
    public async Task NacksMessage_WhenSecondLevelRetryIsCancelledDuringShutdown()
    {
        var settings = new RetryStrategySettings(maxDeliveryAttempts: 1, secondLevelRetriesEnabled: true);
        var transactionContext = new RecordingTransactionContext();
        using var shutdown = new CancellationTokenSource();

        var step = CreateStep(settings, new ThrowingErrorHandler(), CreateErrorTracker(settings), shutdown.Token);

        var calls = 0;

        Task Next()
        {
            if (++calls == 1) throw new InvalidOperationException("handler failed");

            // shutdown begins while the 2nd level handler is running
            shutdown.Cancel();
            throw new OperationCanceledException(shutdown.Token);
        }

        await ProcessIgnoringExceptions(step, CreateContext(Headers(), transactionContext), Next);

        Assert.That(calls, Is.EqualTo(2), "Expected the 2nd level retry to be dispatched");
        Assert.That(transactionContext.Results, Is.EqualTo(new[] { (commit: false, ack: false) }),
            "Expected the retry step to NACK the message when the 2nd level retry is cancelled, like it does for the 1st level");
    }

    static Dictionary<string, string> Headers(params (string key, string value)[] additionalHeaders)
    {
        var headers = new Dictionary<string, string> { [Rebus.Messages.Headers.MessageId] = MessageId };

        foreach (var (key, value) in additionalHeaders)
        {
            headers[key] = value;
        }

        return headers;
    }

    static Func<Task> AlwaysThrow(Func<Exception> exceptionFactory) => () => throw exceptionFactory();

    static Func<Task> Succeed() => () => Task.CompletedTask;

    static async Task ProcessIgnoringExceptions(DefaultRetryStep step, IncomingStepContext context, Func<Task> next)
    {
        // whether the step rethrows is not what is under test - what matters is the result it leaves on the transaction context
        try
        {
            await step.Process(context, next);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Retry step threw: {exception.GetType().Name}: {exception.Message}");
        }
    }

    static IncomingStepContext CreateContext(Dictionary<string, string> headers, ITransactionContext transactionContext) =>
        new(new TransportMessage(headers, Array.Empty<byte>()), transactionContext);

    static InMemErrorTracker CreateErrorTracker(RetryStrategySettings settings)
    {
        var loggerFactory = new ListLoggerFactory();

        return new InMemErrorTracker(
            settings,
            new TplAsyncTaskFactory(loggerFactory),
            new DefaultRebusTime(),
            new DefaultExceptionLogger(loggerFactory),
            new InMemExceptionInfoFactory());
    }

    static DefaultRetryStep CreateStep(RetryStrategySettings settings, IErrorHandler errorHandler, IErrorTracker errorTracker, CancellationToken cancellationToken) =>
        new(
            new ListLoggerFactory(outputToConsole: true),
            errorHandler,
            errorTracker,
            new FailFastChecker(),
            new InMemExceptionInfoFactory(),
            settings,
            cancellationToken);

    class ThrowingErrorHandler : IErrorHandler
    {
        public int Invocations { get; private set; }

        public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception)
        {
            Invocations++;
            throw new InvalidOperationException("Error handler failed, e.g. because it depends on the same unavailable service as the handler");
        }
    }

    class RecordingTransactionContext : ITransactionContext
    {
        public List<(bool commit, bool ack)> Results { get; } = new();

        public ConcurrentDictionary<string, object> Items { get; } = new();

        public void SetResult(bool commit, bool ack) => Results.Add((commit, ack));

        public void OnCommit(Func<ITransactionContext, Task> commitAction) { }
        public void OnRollback(Func<ITransactionContext, Task> rollbackAction) { }
        public void OnAck(Func<ITransactionContext, Task> ackAction) { }
        public void OnNack(Func<ITransactionContext, Task> nackAction) { }
        public void OnDisposed(Action<ITransactionContext> disposedAction) { }
        public void Dispose() { }
    }
}
