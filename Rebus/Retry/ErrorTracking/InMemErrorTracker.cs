using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Retry.Simple;
using Rebus.Threading;
using Rebus.Time;
using Rebus.Transport;
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

    readonly ILog _log;
    readonly SimpleRetryStrategySettings _simpleRetryStrategySettings;
    readonly ITransport _transport;
    readonly IRebusTime _rebusTime;
    readonly ConcurrentDictionary<string, ErrorTracking> _trackedErrors = new ConcurrentDictionary<string, ErrorTracking>();
    readonly IAsyncTask _cleanupOldTrackedErrorsTask;

    bool _disposed;

    /// <summary>
    /// Constructs the in-mem error tracker with the configured number of delivery attempts as the MAX
    /// </summary>
    public InMemErrorTracker(SimpleRetryStrategySettings simpleRetryStrategySettings, IRebusLoggerFactory rebusLoggerFactory, IAsyncTaskFactory asyncTaskFactory, ITransport transport, IRebusTime rebusTime)
    {
        if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
        if (asyncTaskFactory == null) throw new ArgumentNullException(nameof(asyncTaskFactory));

        _simpleRetryStrategySettings = simpleRetryStrategySettings ?? throw new ArgumentNullException(nameof(simpleRetryStrategySettings));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));

        _log = rebusLoggerFactory.GetLogger<InMemErrorTracker>();

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
        // if it's a one-way client, then there's no reason to start the task
        if (string.IsNullOrWhiteSpace(_transport.Address)) return;

        _cleanupOldTrackedErrorsTask.Start();
    }

    /// <summary>
    /// Marks the given <paramref name="messageId"/> as "FINAL", meaning that it should be considered as "having failed too many times now"
    /// </summary>
    public void MarkAsFinal(string messageId)
    {
        _trackedErrors.AddOrUpdate(messageId,
            id => new ErrorTracking(_rebusTime, final: true),
            (id, tracking) => tracking.MarkAsFinal());
    }

    /// <summary>
    /// Registers the given <paramref name="exception"/> under the supplied <paramref name="messageId"/>
    /// </summary>
    public void RegisterError(string messageId, Exception exception)
    {
        var errorTracking = _trackedErrors.AddOrUpdate(messageId,
            id => new ErrorTracking(_rebusTime, exception),
            (id, tracking) => tracking.AddError(_rebusTime, exception, tracking.Final));

        var message = errorTracking.Final
            ? "Unhandled exception {errorNumber} (FINAL) while handling message with ID {messageId}"
            : "Unhandled exception {errorNumber} while handling message with ID {messageId}";

        _log.Warn(exception, message, errorTracking.Errors.Count(), messageId);
    }

    /// <summary>
    /// Gets whether too many errors have been tracked for the given <paramref name="messageId"/>
    /// </summary>
    public bool HasFailedTooManyTimes(string messageId)
    {
        var hasTrackingForThisMessage = _trackedErrors.TryGetValue(messageId, out var existingTracking);
        if (!hasTrackingForThisMessage) return false;

        var hasFailedTooManyTimes = existingTracking.Final
                                    || existingTracking.ErrorCount >= _simpleRetryStrategySettings.MaxDeliveryAttempts;

        return hasFailedTooManyTimes;
    }

    /// <summary>
    /// Gets a short description of the tracked errors for the given <paramref name="messageId"/> on the form
    /// "n unhandled exceptions"
    /// </summary>
    public string GetShortErrorDescription(string messageId)
    {
        return _trackedErrors.TryGetValue(messageId, out var errorTracking)
            ? $"{errorTracking.Errors.Count()} unhandled exceptions"
            : null;
    }

    /// <summary>
    /// Gets a long and detailed description of the tracked errors for the given <paramref name="messageId"/>
    /// consisting of time and full exception details for all registered exceptions
    /// </summary>
    public string GetFullErrorDescription(string messageId)
    {
        if (!_trackedErrors.TryGetValue(messageId, out var errorTracking))
        {
            return null;
        }

        var fullExceptionInfo = string.Join(Environment.NewLine, errorTracking.Errors.Select(e =>
            $"{e.Time}: {e.Exception}"));

        return $"{errorTracking.Errors.Count()} unhandled exceptions: {fullExceptionInfo}";
    }

    /// <summary>
    /// Gets all caught exceptions for the message ID
    /// </summary>
    public IEnumerable<Exception> GetExceptions(string messageId)
    {
        if (!_trackedErrors.TryGetValue(messageId, out var errorTracking))
        {
            return Enumerable.Empty<Exception>();
        }

        return errorTracking.Errors
            .Select(e => e.Exception)
            .ToList();
    }

    /// <summary>
    /// Cleans up whichever tracking wr have done for the given <paramref name="messageId"/>
    /// </summary>
    public void CleanUp(string messageId) => RemoveTracking(messageId);

    async Task CleanupOldTrackedErrors()
    {
        var maxAge = TimeSpan.FromMinutes(_simpleRetryStrategySettings.ErrorTrackingMaxAgeMinutes);

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

    class ErrorTracking
    {
        readonly IRebusTime _rebusTime;
        readonly CaughtException[] _caughtExceptions;

        ErrorTracking(IRebusTime rebusTime, IEnumerable<CaughtException> caughtExceptions, bool final = false)
        {
            _rebusTime = rebusTime;
            Final = final;
            _caughtExceptions = caughtExceptions.ToArray();
        }

        public ErrorTracking(IRebusTime rebusTime, Exception exception = null, bool final = false)
            : this(rebusTime, exception != null ? new[] { new CaughtException(rebusTime.Now, exception) } : new CaughtException[0], final)
        {
        }

        public int ErrorCount => _caughtExceptions.Length;

        public bool Final { get; }

        public IEnumerable<CaughtException> Errors => _caughtExceptions;

        public ErrorTracking AddError(IRebusTime rebusTime, Exception caughtException, bool final)
        {
            //// don't change anymore if this one is already final
            //if (Final) return this;

            return new ErrorTracking(rebusTime, _caughtExceptions.Concat(new[] { new CaughtException(_rebusTime.Now, caughtException) }), final);
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

        public ErrorTracking MarkAsFinal() => new ErrorTracking(_rebusTime, _caughtExceptions, final: true);
    }

    class CaughtException
    {
        public CaughtException(DateTimeOffset time, Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Time = time;
        }

        public Exception Exception { get; }
        public DateTimeOffset Time { get; }
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