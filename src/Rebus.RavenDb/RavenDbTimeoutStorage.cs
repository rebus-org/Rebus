using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client;
using Raven.Client.Linq;
using Rebus.Timeout;

namespace Rebus.RavenDb
{
    public class RavenDbTimeoutStorage : IStoreTimeouts
    {
        readonly IDocumentStore store;

        public RavenDbTimeoutStorage(IDocumentStore store)
        {
            this.store = store;
        }

        public void Add(Timeout.Timeout newTimeout)
        {
            using (var session = store.OpenSession())
            {
                session.Store(new RavenTimeout
                                  {
                                      SagaId = newTimeout.SagaId,
                                      CorrelationId = newTimeout.CorrelationId,
                                      Time = newTimeout.TimeToReturn,
                                      Data = newTimeout.CustomData,
                                      ReplyTo = newTimeout.ReplyTo,
                                  });
                session.SaveChanges();
            }
        }

        public IEnumerable<Timeout.Timeout> RemoveDueTimeouts()
        {
            using (var session = store.OpenSession())
            {
                var dueTimeouts = session.Query<RavenTimeout>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                    .Where(x => x.Time <= Time.Now()).OrderBy(x => x.Time)
                    .ToList();

                foreach (var timeout in dueTimeouts)
                {
                    var loadedTimeout = session.Load<RavenTimeout>(timeout.Id);
                    session.Delete(loadedTimeout);
                }

                var rebusTimeouts = dueTimeouts.Select(storedTimeout =>
                                                       new Timeout.Timeout

                                                           {
                                                               CorrelationId = storedTimeout.CorrelationId,
                                                               SagaId = storedTimeout.SagaId,
                                                               TimeToReturn = storedTimeout.Time,
                                                               CustomData = storedTimeout.Data,
                                                               ReplyTo = storedTimeout.ReplyTo
                                                           }).ToList();
                session.SaveChanges();
                return rebusTimeouts;
            }
        }

        class RavenTimeout
        {
            public string Id { get; set; }
            public Guid SagaId { get; set; }
            public string CorrelationId { get; set; }
            public DateTime Time { get; set; }
            public string Data { get; set; }
            public string ReplyTo { get; set; }
        }
    }
}