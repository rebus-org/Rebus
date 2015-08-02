using Raven.Abstractions.Commands;
using Raven.Client;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Timeouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rebus.RavenDb.Timouts
{
    public class RavenDbTimeoutManager : ITimeoutManager
    {
        private readonly IDocumentStore _documentStore;
        private static ILog _log;

        static RavenDbTimeoutManager()
        {
            RebusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        public RavenDbTimeoutManager(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task Defer(DateTimeOffset approximateDueTime, Dictionary<string, string> headers, byte[] body)
        {
            var newTimeout = new Timeout(headers, body, approximateDueTime.UtcDateTime);
            _log.Debug("Deferring message with ID {0} until {1} (doc ID {2})", headers.GetValue(Headers.MessageId), approximateDueTime, newTimeout.Id);

            using (var session = _documentStore.OpenAsyncSession())
            {
                await session.StoreAsync(newTimeout);
                await session.SaveChangesAsync();
            }
        }

        public async Task<DueMessagesResult> GetDueMessages()
        {
            var now = RebusTime.Now.UtcDateTime;

            DueMessagesResult dueMessagesResult;
            using (var session = _documentStore.OpenAsyncSession())
            {
                var timeouts = await session.Query<Timeout, TimeoutIndex>()
                                            .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromMilliseconds(1000)))
                                            .Where(x => x.DueTimeUtc <= now)
                                            .ToListAsync();

                var dueMessages = timeouts.Select(x => new DueMessage(x.Headers, x.Body, CreateCleanUpProcedure(x.Id)));

                dueMessagesResult = new DueMessagesResult(dueMessages);
            }

            return dueMessagesResult;
        }

        public Action CreateCleanUpProcedure(string id)
        {
            if (string.IsNullOrEmpty(id)) return () => { };

            return () =>
            {
                using (var session = _documentStore.OpenSession())
                {
                    var deleteCommands = new DeleteCommandData { Key = id };
                    session.Advanced.Defer(deleteCommands);
                    session.SaveChanges();
                }
            };
        }
    }
}