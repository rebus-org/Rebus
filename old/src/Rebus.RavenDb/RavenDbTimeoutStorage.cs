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

        public DueTimeoutsResult GetDueTimeouts()
        {
            using (var session = store.OpenSession())
            {
                var now = RebusTimeMachine.Now();

                var dueTimeouts = session.Query<RavenTimeout>()
                    .Customize(x => x.WaitForNonStaleResultsAsOfLastWrite())
                    .Where(x => x.Time <= now).OrderBy(x => x.Time)
                    .ToList();

                var rebusTimeouts = dueTimeouts
                    .Select(storedTimeout =>
                            new DueRavenTimeout(storedTimeout.ReplyTo,
                                                storedTimeout.CorrelationId,
                                                storedTimeout.Time,
                                                storedTimeout.SagaId,
                                                storedTimeout.Data,
                                                store,
                                                storedTimeout.Id))
                    .ToList();

                session.SaveChanges();

                return new DueTimeoutsResult(rebusTimeouts);
            }
        }

        class DueRavenTimeout : DueTimeout
        {
            readonly IDocumentStore documentStore;
            readonly string id;

            public DueRavenTimeout(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData, IDocumentStore documentStore, string id) 
                : base(replyTo, correlationId, timeToReturn, sagaId, customData)
            {
                this.documentStore = documentStore;
                this.id = id;
            }

            public override void MarkAsProcessed()
            {
                using (var session = documentStore.OpenSession())
                {
                    session.Delete(session.Load<RavenTimeout>(id));

                    session.SaveChanges();
                }
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