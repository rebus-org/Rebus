using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Timeouts;
#pragma warning disable 1998

namespace Rebus.Persistence.InMem
{
    /// <summary>
    /// Implementation of <see cref="ITimeoutManager"/> that "persists" timeouts in memory.
    /// </summary>
    public class InMemoryTimeoutManager : ITimeoutManager
    {
        readonly ConcurrentDictionary<string, DeferredMessage> _deferredMessages = new ConcurrentDictionary<string, DeferredMessage>();

        /// <summary>
        /// Stores the message with the given headers and body data, delaying it until the specified <paramref name="approximateDueTime"/>
        /// </summary>
        public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
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
                    .Where(v => RebusTime.Now >= v.Value.DueTime)
                    .ToHashSet();

                var result = new DueMessagesResult(keyValuePairsToRemove
                    .Select(kvp => new DueMessage(kvp.Value.Headers, kvp.Value.Body,
                        () => keyValuePairsToRemove.Remove(kvp))),
                    () =>
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

        class DeferredMessage
        {
            public DateTimeOffset DueTime { get; private set; }
            public Dictionary<string, string> Headers { get; private set; }
            public byte[] Body { get; private set; }

            public DeferredMessage(DateTimeOffset dueTime, Dictionary<string, string> headers, byte[] body)
            {
                DueTime = dueTime;
                Headers = headers;
                Body = body;
            }
        }
    }
}