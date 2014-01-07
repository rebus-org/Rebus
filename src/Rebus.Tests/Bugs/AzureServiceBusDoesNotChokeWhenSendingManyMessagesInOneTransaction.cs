using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Ponder;
using Rebus.AzureServiceBus;
using Rebus.AzureServiceBus.SingleTopic;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Tests.Contracts.Transports.Factories;
using Rebus.Tests.Persistence;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Category(TestCategories.Azure)]
    public class AzureServiceBusDoesNotChokeWhenSendingManyMessagesInOneTransaction : FixtureBase
    {
        const string SagaTable = "many_msg_sagas";
        const string SagaIndex = "many_msg_saga_index";
        const string InputQueueName = "test_many_msg";

        BuiltinContainerAdapter adapter;

        ManualResetEvent allRepliesReceived;
        SqlServerSagaPersister sagaPersister;

        protected override void DoSetUp()
        {
            SqlServerFixtureBase.DropTable(SagaIndex);
            SqlServerFixtureBase.DropTable(SagaTable);

            var busConnection = AzureServiceBusMessageQueueFactory.ConnectionString;
            var sqlConnection = SqlServerFixtureBase.ConnectionString;

            using (var azureQueue = new AzureServiceBusMessageQueue(busConnection, InputQueueName))
            {
                azureQueue.Purge();
            }

            allRepliesReceived = new ManualResetEvent(false);

            adapter = new BuiltinContainerAdapter();
            adapter.Register(() => new RequestHandler(adapter.Bus));

            sagaPersister = new SqlServerSagaPersister(sqlConnection, SagaIndex, SagaTable).EnsureTablesAreCreated();

            Configure.With(TrackDisposable(adapter))
                .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                .Transport(t => t.UseAzureServiceBus(busConnection, InputQueueName, "error"))
                .Sagas(s => s.Use(sagaPersister))
                .CreateBus()
                .Start(3);
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(20)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(150, Ignore = true, Description="Max batch size within tx=100")]
        [TestCase(200, Ignore = true, Description="Max batch size within tx=100")]
        public void RunIt(int requestCount)
        {
            // arrange
            const string initiatingString = "Hello!";
            adapter.Register(() => new SomeSaga(adapter.Bus, allRepliesReceived, requestCount));

            // act
            adapter.Bus.SendLocal(initiatingString);

            // assert
            var timeout = 5.Seconds() + (requestCount / 2).Seconds();
            allRepliesReceived.WaitUntilSetOrDie(timeout, "Did not receive all replies within {0} timeout", timeout);

            Thread.Sleep(2.Seconds());

            var sagaDataPropertyPath = Reflect.Path<SomeSagaData>(d => d.InitiatingString);

            var data = sagaPersister.Find<SomeSagaData>(sagaDataPropertyPath, initiatingString);
            
            Assert.That(data, Is.Not.Null, "Could not find saga data!!!");
            Assert.That(data.Requests.Count, Is.EqualTo(requestCount));
            Assert.That(data.Requests.All(r => r.Value == 1), "The following requests were not replied to exactly once: {0}",
                string.Join(", ", data.Requests.Where(kvp => kvp.Value != 1).Select(kvp => string.Format("{0} ({1})", kvp.Key, kvp.Value))));
        }


        class SomeSaga : Saga<SomeSagaData>,
            IAmInitiatedBy<string>,
            IHandleMessages<SomeReply>
        {
            readonly IBus bus;
            readonly ManualResetEvent allRepliesReceived;
            readonly int requestCount;

            public SomeSaga(IBus bus, ManualResetEvent allRepliesReceived, int requestCount)
            {
                this.bus = bus;
                this.allRepliesReceived = allRepliesReceived;
                this.requestCount = requestCount;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<string>(s => s).CorrelatesWith(d => d.InitiatingString);
                Incoming<SomeReply>(r => r.CorrelationId).CorrelatesWith(d => d.CorrelationId);
            }

            public void Handle(string message)
            {
                if (!IsNew) return;

                Console.WriteLine("Starting saga for {0}", message);

                Data.InitiatingString = message;
                Data.CorrelationId = Guid.NewGuid();

                Console.WriteLine("Sending {0} requests", requestCount);

                Enumerable.Range(0, requestCount)
                    .ToList()
                    .ForEach(i =>
                    {
                        Data.Requests[i] = 0;
                        bus.SendLocal(new SomeRequest
                        {
                            CorrelationId = Data.CorrelationId,
                            RequestId = i
                        });
                    });
            }

            public void Handle(SomeReply message)
            {
                Console.WriteLine("Got reply for request ID {0}", message.RequestId);
                Data.Requests[message.RequestId]++;

                if (Data.Requests.All(kvp => kvp.Value > 0))
                {
                    Console.WriteLine("All replies received!");
                    allRepliesReceived.Set();
                }
            }
        }

        class SomeSagaData : ISagaData
        {
            public SomeSagaData()
            {
                Requests = new Dictionary<int, int>();
            }
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public Guid CorrelationId { get; set; }
            public string InitiatingString { get; set; }
            public Dictionary<int, int> Requests { get; set; }
        }

        class SomeRequest
        {
            public Guid CorrelationId { get; set; }
            public int RequestId { get; set; }
        }
        class SomeReply
        {
            public Guid CorrelationId { get; set; }
            public int RequestId { get; set; }
        }

        class RequestHandler : IHandleMessages<SomeRequest>
        {
            readonly IBus bus;

            public RequestHandler(IBus bus)
            {
                this.bus = bus;
            }

            public void Handle(SomeRequest message)
            {
                bus.Reply(new SomeReply
                {
                    CorrelationId = message.CorrelationId,
                    RequestId = message.RequestId,
                });
            }
        }
    }
}