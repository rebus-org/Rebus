using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Retry;
using Rebus.Retry.FailFast;
using Rebus.Retry.Simple;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport;

namespace Rebus.Tests.Retry.Simple;

[TestFixture]
public class TestDefaultRetryStep
{
    [Test]
    public async Task CreatesInfoForEmptyMessageException()
    {
        var exceptionInfoFactory = new FakeExceptionInfoFactory();
        var step = CreateDefaultRetryStep(exceptionInfoFactory: exceptionInfoFactory);
        var context = CreateContext();

        await step.Process(context, () => Task.CompletedTask);

        Assert.That(exceptionInfoFactory.CreatedExceptions, Has.Count.EqualTo(1));
        Assert.That(exceptionInfoFactory.CreatedExceptions[0], Is.InstanceOf<RebusApplicationException>());
        Assert.That(exceptionInfoFactory.CreatedExceptions[0].Message, Does.Contain("empty"));
    }

    [Test]
    public async Task CreatesInfoForStepExceptionWhenShouldFailFast()
    {
        var exceptionInfoFactory = new FakeExceptionInfoFactory();
        var failFastChecker = new FakeFailFastChecker();
        var nextException = new InvalidOperationException("Test exception");
        var messageId = "test-message-id";

        failFastChecker.SetShouldFailFast(messageId, nextException, true);

        var step = CreateDefaultRetryStep(
            exceptionInfoFactory: exceptionInfoFactory,
            failFastChecker: failFastChecker);

        var context = CreateContext(messageId);

        await step.Process(context, () => throw nextException);

        Assert.That(exceptionInfoFactory.CreatedExceptions, Has.Count.EqualTo(1));
        Assert.That(exceptionInfoFactory.CreatedExceptions[0], Is.SameAs(nextException));
    }

    [Test]
    public async Task CreatesInfoForSecondLevelRetryException()
    {
        var exceptionInfoFactory = new FakeExceptionInfoFactory();
        var errorHandler = new FakeErrorHandler();
        var errorTracker = new FakeErrorTracker();
        var failFastChecker = new FakeFailFastChecker();
        var messageId = "test-message-id";
        var firstException = new InvalidOperationException("First exception");
        var secondException = new InvalidOperationException("Second exception");

        int callCount = 0;
        var exceptions = new[] { firstException, secondException };

        failFastChecker.SetDefaultShouldFailFast(false);
        errorTracker.SetHasFailedTooManyTimes(messageId, true);

        var step = new DefaultRetryStep(
            new ListLoggerFactory(),
            errorHandler,
            errorTracker,
            failFastChecker,
            exceptionInfoFactory,
            new RetryStrategySettings(secondLevelRetriesEnabled: true),
            CancellationToken.None);

        var context = CreateContext(messageId);

        await step.Process(context, () => throw exceptions[callCount++]);

        Assert.That(exceptionInfoFactory.CreatedExceptions, Has.Count.EqualTo(1));
        Assert.That(exceptionInfoFactory.CreatedExceptions[0], Is.SameAs(secondException));
    }

    private static DefaultRetryStep CreateDefaultRetryStep(
        IRebusLoggerFactory loggerFactory = null,
        IErrorHandler errorHandler = null,
        IErrorTracker errorTracker = null,
        IFailFastChecker failFastChecker = null,
        IExceptionInfoFactory exceptionInfoFactory = null,
        RetryStrategySettings retryStrategySettings = null)
    {
        return new DefaultRetryStep(
            loggerFactory ?? new ListLoggerFactory(),
            errorHandler ?? new FakeErrorHandler(),
            errorTracker ?? new FakeErrorTracker(),
            failFastChecker ?? new FakeFailFastChecker(),
            exceptionInfoFactory ?? new FakeExceptionInfoFactory(),
            retryStrategySettings ?? new RetryStrategySettings(),
            CancellationToken.None);
    }

    private static IncomingStepContext CreateContext(string messageId = null)
    {
        var headers = new Dictionary<string, string>();
        if (messageId != null)
        {
            headers[Headers.MessageId] = messageId;
        }
        var transportMessage = new TransportMessage(headers, new byte[0]);
        var transactionContext = new FakeTransactionContext();
        return new IncomingStepContext(transportMessage, transactionContext);
    }

    class FakeTransactionContext : ITransactionContext
    {
        public ConcurrentDictionary<string, object> Items { get; } = new();

        public void OnCommit(Func<ITransactionContext, Task> commitAction) { }
        public void OnRollback(Func<ITransactionContext, Task> abortedAction) { }
        public void OnAck(Func<ITransactionContext, Task> completedAction) { }
        public void OnNack(Func<ITransactionContext, Task> commitAction) { }
        public void OnDisposed(Action<ITransactionContext> disposedAction) { }
        public void SetResult(bool commit, bool ack) { }
        public void Dispose() { }
    }

    class FakeExceptionInfoFactory : IExceptionInfoFactory
    {
        public List<Exception> CreatedExceptions { get; } = new();

        public ExceptionInfo CreateInfo(Exception exception)
        {
            CreatedExceptions.Add(exception);
            return ExceptionInfo.FromException(exception);
        }
    }

    class FakeErrorHandler : IErrorHandler
    {
        public Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception)
        {
            return Task.CompletedTask;
        }
    }

    class FakeErrorTracker : IErrorTracker
    {
        private readonly Dictionary<string, bool> _hasFailedTooManyTimesResults = new();

        public void SetHasFailedTooManyTimes(string messageId, bool result)
        {
            _hasFailedTooManyTimesResults[messageId] = result;
        }

        public Task<bool> HasFailedTooManyTimes(string messageId)
        {
            return Task.FromResult(_hasFailedTooManyTimesResults.TryGetValue(messageId, out var result) && result);
        }

        public Task RegisterError(string messageId, Exception exception)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetShortErrorDescription(string messageId)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<string> GetFullErrorDescription(string messageId)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<IReadOnlyList<ExceptionInfo>> GetExceptions(string messageId)
        {
            return Task.FromResult<IReadOnlyList<ExceptionInfo>>(new List<ExceptionInfo>());
        }

        public Task MarkAsFinal(string messageId)
        {
            return Task.CompletedTask;
        }

        public Task CleanUp(string messageId)
        {
            return Task.CompletedTask;
        }
    }

    class FakeFailFastChecker : IFailFastChecker
    {
        private readonly Dictionary<(string, Exception), bool> _shouldFailFastResults = new();
        private bool _defaultResult = false;

        public void SetShouldFailFast(string messageId, Exception exception, bool result)
        {
            _shouldFailFastResults[(messageId, exception)] = result;
        }

        public void SetDefaultShouldFailFast(bool result)
        {
            _defaultResult = result;
        }

        public bool ShouldFailFast(string messageId, Exception exception)
        {
            return _shouldFailFastResults.TryGetValue((messageId, exception), out var result) ? result : _defaultResult;
        }
    }
}
