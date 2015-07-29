using System;
using System.Threading;
using NUnit.Framework;
using Raven.Client.Embedded;
using Rebus.Persistence.InMemory;
using Rebus.RavenDb;

namespace Rebus.Tests.Bugs
{
    public class SagasNotWorkingWithRavenDbEnlistingInTransaction : RebusBusMsmqIntegrationTestBase
    {
        private const string Queue = "test.publisher";

        [Test]
        [Ignore("This seems to be a bug related to RavenDb inmem store and transaction. Investigating further.")]
        public void ShouldWork()
        {
            var store = new EmbeddableDocumentStore
                            {
                                RunInMemory = true
                            };

            store.Initialize();

            var activator = new HandlerActivatorForTesting();
            var checker = new CheckCallsMade();
            var bus = CreateBus(Queue, activator, new InMemorySubscriptionStorage(), new RavenDbSagaPersister(store), "errors").Start(1);
            activator.UseHandler(() => new TheSaga(bus, checker));
            bus.Send(new TheFirstMessage());

            Thread.Sleep(5000);
            Assert.IsTrue(checker.First, "First should be called");
            Assert.IsTrue(checker.Second, "Second should be called");
            Assert.IsTrue(checker.Third, "Third should be called");
        }

        public override string GetEndpointFor(Type messageType)
        {
            return Queue;
        }

        public class TheFirstMessage
        {
        }

        public class TheSaga : Saga<TheSaga.SomeSagaData>,
                               IAmInitiatedBy<TheFirstMessage>,
                               IHandleMessages<TheSecondMessage>,
                               IHandleMessages<TheThirdMessage>
        {
            private readonly IBus bus;
            private readonly CheckCallsMade checker;

            public TheSaga(IBus bus, CheckCallsMade checker)
            {
                this.bus = bus;
                this.checker = checker;
            }

            public void Handle(TheFirstMessage message)
            {
                checker.First = true;
                bus.SendLocal(new TheSecondMessage
                                  {
                                      CorrelationId = Data.Id,
                                  });
            }

            public void Handle(TheSecondMessage message)
            {
                checker.Second = true;
                Data.SomeOtherField = "Asger";
                bus.SendLocal(new TheThirdMessage
                                  {
                                      CorrelationId = Data.Id,
                                  });
            }

            public void Handle(TheThirdMessage message)
            {
                checker.Third = true;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<TheSecondMessage>(x => x.CorrelationId).CorrelatesWith(x => x.Id);
                Incoming<TheThirdMessage>(x => x.CorrelationId).CorrelatesWith(x => x.Id);
            }

            public class SomeSagaData : ISagaData
            {
                public SomeSagaData()
                {
                    Id = Guid.NewGuid();
                }

                public string SomeOtherField { get; set; }

                public Guid Id { get; set; }
                public int Revision { get; set; }
            }
        }

        public class TheSecondMessage
        {
            public Guid CorrelationId { get; set; }
        }

        public class TheThirdMessage
        {
            public Guid CorrelationId { get; set; }
        }
    }
}