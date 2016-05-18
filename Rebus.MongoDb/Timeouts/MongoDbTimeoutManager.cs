using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Timeouts;

namespace Rebus.MongoDb.Timeouts
{
    /// <summary>
    /// Implementation of <see cref="ITimeoutManager"/> that uses MongoDB to save timeouts
    /// </summary>
    public class MongoDbTimeoutManager : ITimeoutManager
    {
        readonly IMongoCollection<Timeout> _timeouts;
        readonly ILog _log;

        /// <summary>
        /// Constructs the timeout manager
        /// </summary>
        public MongoDbTimeoutManager(IMongoDatabase database, string collectionName, IRebusLoggerFactory rebusLoggerFactory)
        {
            _log = rebusLoggerFactory.GetCurrentClassLogger();
            _timeouts = database.GetCollection<Timeout>(collectionName);
        }

        public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
            var newTimeout = new Timeout(headers, body, approximateDueTime.UtcDateTime);
            _log.Debug("Deferring message with ID {0} until {1} (doc ID {2})", headers.GetValue(Headers.MessageId), approximateDueTime, newTimeout.Id);
            await _timeouts.InsertOneAsync(newTimeout).ConfigureAwait(false);
        }

        public async Task<DueMessagesResult> GetDueMessages()
        {
            var now = RebusTime.Now.UtcDateTime;
            var dueTimeouts = new List<Timeout>();

            while (dueTimeouts.Count < 100)
            {
                var dueTimeout = await _timeouts.FindOneAndUpdateAsync(Builders<Timeout>.Filter.Lte(t => t.DueTimeUtc, now),
                                            Builders<Timeout>.Update.Set(t => t.DueTimeUtc, now.AddMinutes(1))).ConfigureAwait(false);

                if (dueTimeout == null)
                {
                    break;
                }

                dueTimeouts.Add(dueTimeout);
            }

            var timeoutsNotCompleted = dueTimeouts.ToDictionary(t => t.Id);

            var dueMessages = dueTimeouts
                .Select(timeout => new DueMessage(timeout.Headers, timeout.Body, async () =>
                {
                    _log.Debug("Completing timeout for message with ID {0} (doc ID {1})", timeout.Headers.GetValue(Headers.MessageId), timeout.Id);
                    await _timeouts.DeleteOneAsync(Builders<Timeout>.Filter.Eq(t => t.Id, timeout.Id)).ConfigureAwait(false);
                    timeoutsNotCompleted.Remove(timeout.Id);
                }))
                .ToList();

            return new DueMessagesResult(dueMessages, async () =>
            {
                foreach (var timeoutNotCompleted in timeoutsNotCompleted.Values)
                {
                    try
                    {
                        _log.Debug("Timeout for message with ID {0} (doc ID {1}) was not completed - will set due time back to {2} now",
                            timeoutNotCompleted.Headers.GetValue(Headers.MessageId), timeoutNotCompleted.Id,
                            timeoutNotCompleted.OriginalDueTimeUtc);

                        await _timeouts.UpdateOneAsync(Builders<Timeout>.Filter.Eq(t => t.Id, timeoutNotCompleted.Id),
                            Builders<Timeout>.Update.Set(t => t.DueTimeUtc, timeoutNotCompleted.OriginalDueTimeUtc)).ConfigureAwait(false);
                    }
                    catch(Exception exception)
                    {
                        _log.Warn("Could not set due time for timeout with doc ID {0}: {1}", timeoutNotCompleted.Id, exception);
                    }
                }
            });
        }

        class Timeout
        {
            public Timeout(Dictionary<string, string> headers, byte[] body, DateTime dueTimeUtc)
            {
                Id = ObjectId.GenerateNewId();
                Headers = headers;
                Body = body;
                DueTimeUtc = dueTimeUtc;
                OriginalDueTimeUtc = dueTimeUtc;
            }

            public ObjectId Id { get; }
            public Dictionary<string, string> Headers { get; }
            public byte[] Body { get; }
            public DateTime DueTimeUtc { get; }
            public DateTime OriginalDueTimeUtc { get; }
        }
    }
}