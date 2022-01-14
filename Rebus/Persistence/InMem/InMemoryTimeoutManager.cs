using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Timeouts;
// ReSharper disable InconsistentlySynchronizedField
#pragma warning disable 1998

namespace Rebus.Persistence.InMem;

/// <summary>
/// Implementation of <see cref="ITimeoutManager"/> that "persists" timeouts in memory.
/// </summary>
public class InMemoryTimeoutManager : ITimeoutManager, IEnumerable<InMemoryTimeoutManager.DeferredMessage>
{
    readonly IRebusTime _rebusTime;
    readonly ConcurrentDictionary<string, DeferredMessage> _deferredMessages = new ConcurrentDictionary<string, DeferredMessage>();

    /// <summary>
    /// Creates the in-mem timeout manager
    /// </summary>
    public InMemoryTimeoutManager(IRebusTime rebusTime)
    {
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
    }

    /// <summary>
    /// Stores the message with the given headers and body data, delaying it until the specified <paramref name="approximateDueTime"/>
    /// </summary>
    public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
    {
        if (headers == null) throw new ArgumentNullException(nameof(headers));
        if (body == null) throw new ArgumentNullException(nameof(body));

        lock (_deferredMessages)
        {
            _deferredMessages
                .AddOrUpdate(headers.GetValue(Headers.MessageId),
                    id => new DeferredMessage(approximateDueTime, headers, body),
                    (id, existing) => existing);
        }
    }

    /// <summary>
    /// Gets due messages as of now, given the approximate due time that they were stored with when <see cref="ITimeoutManager.Defer"/> was called
    /// </summary>
    public async Task<DueMessagesResult> GetDueMessages()
    {
        lock (_deferredMessages)
        {
            var keyValuePairsToRemove = _deferredMessages
                .Where(v => _rebusTime.Now >= v.Value.DueTime)
                .ToHashSet();

            var result = new DueMessagesResult(keyValuePairsToRemove
                    .Select(kvp =>
                    {
                        var dueMessage = new DueMessage(kvp.Value.Headers, kvp.Value.Body,
                            async () => keyValuePairsToRemove.Remove(kvp));

                        return dueMessage;
                    }),
                async () =>
                {
                    // put back if the result was not completed
                    foreach (var kvp in keyValuePairsToRemove)
                    {
                        _deferredMessages[kvp.Key] = kvp.Value;
                    }
                });

            foreach (var kvp in keyValuePairsToRemove)
            {
                DeferredMessage _;
                _deferredMessages.TryRemove(kvp.Key, out _);
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a message whose delivery has been deferred into the future
    /// </summary>
    public class DeferredMessage
    {
        /// <summary>
        /// Gets the time of when delivery of this message is due
        /// </summary>
        public DateTimeOffset DueTime { get; }

        /// <summary>
        /// Gets the message's headers
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the message's body
        /// </summary>
        public byte[] Body { get; }

        internal DeferredMessage(DateTimeOffset dueTime, Dictionary<string, string> headers, byte[] body)
        {
            DueTime = dueTime;
            Headers = headers;
            Body = body;
        }
    }

    /// <summary>
    /// Gets an enumerator that allows for iterating through all stored deferred messages
    /// </summary>
    public IEnumerator<DeferredMessage> GetEnumerator()
    {
        var deferredMessages = _deferredMessages.Values.ToList();

        return deferredMessages.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}