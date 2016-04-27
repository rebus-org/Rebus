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
#pragma warning disable 1998

namespace Rebus.RavenDb.Timouts
{
    /// <summary>
    /// Implementation of <see cref="ITimeoutManager"/> that stores timeouts in RavenDB
    /// </summary>
    public class RavenDbTimeoutManager : ITimeoutManager
    {
        readonly IDocumentStore _documentStore;
        readonly ILog _log;

        /// <summary>
        /// Creates the timeout manager, using the given document store to store <see cref="Timeout"/> documents
        /// </summary>
        public RavenDbTimeoutManager(IDocumentStore documentStore, IRebusLoggerFactory rebusLoggerFactory)
        {
            _documentStore = documentStore;
            _log = rebusLoggerFactory.GetCurrentClassLogger();
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

        /// <summary>
        /// Gets due messages as of now, given the approximate due time that they were stored with when <see cref="ITimeoutManager.Defer"/> was called
        /// </summary>
        public async Task<DueMessagesResult> GetDueMessages()
        {
            var now = RebusTime.Now.UtcDateTime;

            var session = _documentStore.OpenSession();

            var timeouts = session.Query<Timeout, TimeoutIndex>()
                .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(10)))
                .Where(x => x.DueTimeUtc <= now)
                .ToList();

            var dueMessages = timeouts
                .Select(timeout => new DueMessage(timeout.Headers, timeout.Body, () =>
                {
                    session.Advanced.Defer(new DeleteCommandData {Key = timeout.Id});
                }));

            return new DueMessagesResult(dueMessages, () =>
            {
                try
                {
                    session.SaveChanges();
                }
                finally
                {
                    session.Dispose();
                }
            });
        }
    }
}