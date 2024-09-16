using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Retry.Simple;
using Rebus.Threading;
using Rebus.Time;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable ArgumentsStyleLiteral

#pragma warning disable 1998

namespace Rebus.Retry.ErrorTracking;

/// <summary>
/// Implementation of <see cref="IErrorTracker"/> that tracks errors in an in-mem dictionary
/// </summary>
public class InMemErrorTracker : IErrorTracker, IInitializable, IDisposable
{
    const string BackgroundTaskName = "CleanupTrackedErrors";

    static readonly Task<bool> FalseTaskResult = Task.FromResult(false);

    readonly ConcurrentDictionary<string, ErrorTracking> _trackedErrors = new();
    readonly RetryStrategySettings _retryStrategySettings;
    readonly IAsyncTask _cleanupOldTrackedErrorsTask;
    readonly IExceptionLogger _exceptionLogger;
    readonly IExceptionInfoFactory _exceptionInfoFactory;
    readonly IRebusTime _rebusTime;

    bool _disposed;

    /// <summary>
    /// Constructs the in-mem error tracker with the configured number of delivery attempts as the MAX
    /// </summary>
    public InMemErrorTracker(RetryStrategySettings retryStrategySettings, IAsyncTaskFactory asyncTaskFactory, IRebusTime rebusTime, IExceptionLogger exceptionLogger, IExceptionInfoFactory exceptionInfoFactory)
    {
        if (asyncTaskFactory == null) throw new ArgumentNullException(nameof(asyncTaskFactory));

        _retryStrategySettings = retryStrategySettings ?? throw new ArgumentNullException(nameof(retryStrategySettings));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
        _exceptionLogger = exceptionLogger ?? throw new ArgumentNullException(nameof(exceptionLogger));
        _exceptionInfoFactory = exceptionInfoFactory ?? throw new ArgumentNullException(nameof(exceptionInfoFactory));

        _cleanupOldTrackedErrorsTask = asyncTaskFactory.Create(
            BackgroundTaskName,
            CleanupOldTrackedErrors,
            intervalSeconds: 10
        );
    }

    /// <summary>
    /// Initializes the in-mem error tracker - starts a background task that periodically cleans up tracked errors that haven't had any activity for 10 minutes or more
    /// </summary>
    public void Initialize()
    {
        _cleanupOldTrackedErrorsTask.Start();
    }

    /// <summary>
    /// Marks the given <paramref name="messageId"/> as "FINAL", meaning that it should be considered as "having failed too many times now"
    /// </summary>
    public Task MarkAsFinal(string messageId)
    {
        _trackedErrors.AddOrUpdate(messageId,
            _ => new ErrorTracking(_rebusTime, final: true),
            (_, tracking) => tracking.MarkAsFinal());

        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers the given <paramref name="exception"/> under the supplied <paramref name="messageId"/>
    /// </summary>
    public Task RegisterError(string messageId, Exception exception)
    {
        var errorTracking = _trackedErrors.AddOrUpdate(messageId,
            id => new ErrorTracking(_rebusTime, _exceptionInfoFactory.CreateInfo(exception)),
            (id, tracking) => tracking.AddError(_rebusTime, _exceptionInfoFactory.CreateInfo(exception), tracking.Final));

        _exceptionLogger.LogException(messageId, exception, errorTracking.ErrorCount, errorTracking.Final);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets whether too many errors have been tracked for the given <paramref name="messageId"/>
    /// </summary>
    public Task<bool> HasFailedTooManyTimes(string messageId)
    {
        var hasTrackingForThisMessage = _trackedErrors.TryGetValue(messageId, out var existingTracking);
        if (!hasTrackingForThisMessage) return FalseTaskResult;

        var hasFailedTooManyTimes = existingTracking.Final
                                    || existingTracking.ErrorCount >= _retryStrategySettings.MaxDeliveryAttempts;

        return Task.FromResult(hasFailedTooManyTimes);
    }

    /// <summary>
    /// Gets a long and detailed description of the tracked errors for the given <paramref name="messageId"/>
    /// consisting of time and full exception details for all registered exceptions
    /// </summary>
    public async Task<string> GetFullErrorDescription(string messageId)
    {
        if (!_trackedErrors.TryGetValue(messageId, out var errorTracking))
        {
            return null;
        }

        var fullExceptionInfo = string.Join(Environment.NewLine, errorTracking.Errors.Select(e => e.GetFullErrorDescription()));

        return $"{errorTracking.Errors.Count()} unhandled exceptions: {fullExceptionInfo}";
    }

    /// <summary>
    /// Gets all caught exceptions for the message ID
    /// </summary>
    public async Task<IReadOnlyList<ExceptionInfo>> GetExceptions(string messageId)
    {
        if (!_trackedErrors.TryGetValue(messageId, out var errorTracking))
        {
            return Array.Empty<ExceptionInfo>();
        }

        return errorTracking.Errors.ToList();
    }

    /// <summary>
    /// Cleans up whichever tracking wr have done for the given <paramref name="messageId"/>
    /// </summary>
    public Task CleanUp(string messageId)
    {
        RemoveTracking(messageId);
        return Task.CompletedTask;
    }

    async Task CleanupOldTrackedErrors()
    {
        var maxAge = TimeSpan.FromMinutes(_retryStrategySettings.ErrorTrackingMaxAgeMinutes);

        var messageIdsOfExpiredTrackings = _trackedErrors
            .Where(e => e.Value.ElapsedSinceLastError > maxAge)
            .Select(t => t.Key)
            .ToList();

        foreach (var messageId in messageIdsOfExpiredTrackings)
        {
            RemoveTracking(messageId);
        }
    }

    void RemoveTracking(string messageId) => _trackedErrors.TryRemove(messageId, out _);

    sealed class ErrorTracking
    {
        readonly IRebusTime _rebusTime;
        readonly ExceptionInfo[] _caughtExceptions;

        ErrorTracking(IRebusTime rebusTime, IEnumerable<ExceptionInfo> caughtExceptions, bool final = false)
        {
            _rebusTime = rebusTime;
            Final = final;
            _caughtExceptions = caughtExceptions.ToArray();
        }

        public ErrorTracking(IRebusTime rebusTime, ExceptionInfo exception = null, bool final = false)
            : this(rebusTime, exception != null ? new[] { exception } : Array.Empty<ExceptionInfo>(), final)
        {
        }

        public int ErrorCount => _caughtExceptions.Length;

        public bool Final { get; }

        public IEnumerable<ExceptionInfo> Errors => _caughtExceptions;

        public ErrorTracking AddError(IRebusTime rebusTime, ExceptionInfo caughtException, bool final)
        {
            //// don't change anymore if this one is already final
            //if (Final) return this;

            return new ErrorTracking(rebusTime, _caughtExceptions.Concat(new[] { caughtException, }), final);
        }

        public TimeSpan ElapsedSinceLastError
        {
            get
            {
                var timeOfMostRecentError = _caughtExceptions.Max(e => e.Time);
                var elapsedSinceLastError = timeOfMostRecentError.ElapsedUntilNow(_rebusTime);
                return elapsedSinceLastError;
            }
        }

        public ErrorTracking MarkAsFinal() => new(_rebusTime, _caughtExceptions, final: true);
    }

    /// <summary>
    /// Stops the periodic cleanup of tracked messages
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _cleanupOldTrackedErrorsTask.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }
}