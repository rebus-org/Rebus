using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Timeouts;

namespace Rebus.MongoDb.Timeouts
{
    public class MongoDbTimeoutManager : ITimeoutManager
    {
        static ILog _log;

        static MongoDbTimeoutManager()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly MongoCollection<Timeout> _timeouts;

        public MongoDbTimeoutManager(MongoDatabase database, string collectionName)
        {
            _timeouts = database.GetCollection<Timeout>(collectionName);
        }

        public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
            var newTimeout = new Timeout(headers, body, approximateDueTime.UtcDateTime);
            _log.Debug("Deferring message with ID {0} until {1} (doc ID {2})", headers.GetValue(Headers.MessageId), approximateDueTime, newTimeout.Id);
            _timeouts.Insert(newTimeout);
        }

        public async Task<DueMessagesResult> GetDueMessages()
        {
            var now = RebusTime.Now.UtcDateTime;
            var dueTimeouts = new List<Timeout>();

            while (dueTimeouts.Count < 100)
            {
                var dueTimeoutResult = _timeouts.FindAndModify(new FindAndModifyArgs
                {
                    Query = Query<Timeout>.LTE(t => t.DueTimeUtc, now),
                    Update = Update<Timeout>.Set(t => t.DueTimeUtc, now.AddMinutes(1))
                });

                if (dueTimeoutResult.ModifiedDocument == null) break;

                var dueTimeout = dueTimeoutResult.GetModifiedDocumentAs<Timeout>();
                dueTimeouts.Add(dueTimeout);
            }

            var timeoutsNotCompleted = dueTimeouts.ToDictionary(t => t.Id);

            var dueMessages = dueTimeouts
                .Select(timeout => new DueMessage(timeout.Headers, timeout.Body, () =>
                {
                    _log.Debug("Completing timeout for message with ID {0} (doc ID {1})", timeout.Headers.GetValue(Headers.MessageId), timeout.Id);
                    _timeouts.Remove(Query<Timeout>.EQ(t => t.Id, timeout.Id));
                    timeoutsNotCompleted.Remove(timeout.Id);
                }))
                .ToList();

            return new DueMessagesResult(dueMessages, () =>
            {
                foreach (var timeoutNotCompleted in timeoutsNotCompleted.Values)
                {
                    try
                    {
                        _log.Debug("Timeout for message with ID {0} (doc ID {1}) was not completed - will set due time back to {2} now",
                            timeoutNotCompleted.Headers.GetValue(Headers.MessageId), timeoutNotCompleted.Id,
                            timeoutNotCompleted.OriginalDueTimeUtc);

                        _timeouts.Update(Query<Timeout>.EQ(t => t.Id, timeoutNotCompleted.Id),
                            Update<Timeout>.Set(t => t.DueTimeUtc, timeoutNotCompleted.OriginalDueTimeUtc));
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

            public ObjectId Id { get; protected set; }
            public Dictionary<string, string> Headers { get; protected set; }
            public byte[] Body { get; protected set; }
            public DateTime DueTimeUtc { get; protected set; }
            public DateTime OriginalDueTimeUtc { get; protected set; }
        }
    }
}