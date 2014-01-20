using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Persistence.InMemory;
using Rebus.Persistence.SqlServer;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Tests.Persistence;
using Rebus.Transports.Msmq;
using Rebus.Transports.Sql;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class TestUsingTransactionScope : SqlServerFixtureBase, IDetermineMessageOwnership
    {
        const string PublisherInputQueueName = "test.sub-unsub.publisher";
        const string SubscriberInputQueueName = "test.sub-unsub.subscriber";
        const string SubscriptionsTableName = "subscriptions";

        List<IDisposable> toDispose;

        protected RearrangeHandlersPipelineInspector pipelineInspector = new RearrangeHandlersPipelineInspector();

        protected override void DoSetUp()
        {
            toDispose = new List<IDisposable>();
            DropTable(SubscriptionsTableName);
        }

        protected override void DoTearDown()
        {
            toDispose.ForEach(b => b.Dispose());
        }

        [Test]
        public void CanPublishWithinTransactionScopeWhenProvidingTransactionLessConnectionHolder()
        {
            // arrange
            var subscriptions = new SqlServerSubscriptionStorage(() =>
            {
                var sqlConnection = new SqlConnection(ConnectionString);
                sqlConnection.Open();
                return ConnectionHolder.ForNonTransactionalWork(sqlConnection);
            }
                , SubscriptionsTableName)
                .EnsureTableIsCreated();

            var publisher = CreateBus(PublisherInputQueueName, new HandlerActivatorForTesting(), subscriptions).Start(1);

            var subReceivedEvents = new List<int>();

            var sub = CreateBus(SubscriberInputQueueName,
                                 new HandlerActivatorForTesting()
                                     .Handle<SomeEvent>(e => subReceivedEvents.Add(e.EventNumber)),
                                     subscriptions).Start(1);
            sub.Subscribe<SomeEvent>();

            // act
            Thread.Sleep(1.Seconds());

            using (var scope = new TransactionScope())
            {
                publisher.Publish(new SomeEvent { EventNumber = 1 });
                scope.Complete();
            }

            Thread.Sleep(1.Seconds());

            // assert
            subReceivedEvents.ShouldBe(new[] { 1 }.ToList());
        }

        [Test]
        public void CanPublishWithinTransactionScopeWhenProvidingDefaultConnectionHolder()
        {
            // arrange
            var subscriptionStorage = new SqlServerSubscriptionStorage(ConnectionString, SubscriptionsTableName).EnsureTableIsCreated();
            var publisher = CreateBus(PublisherInputQueueName, new HandlerActivatorForTesting(), subscriptionStorage).Start(1);

            var subReceivedEvents = new List<int>();

            var sub = CreateBus(SubscriberInputQueueName,
                                 new HandlerActivatorForTesting()
                                     .Handle<SomeEvent>(e => subReceivedEvents.Add(e.EventNumber)),
                                     subscriptionStorage).Start(1);
            sub.Subscribe<SomeEvent>();

            // act
            Thread.Sleep(1.Seconds());

            using (var scope = new TransactionScope())
            {
                publisher.Publish(new SomeEvent { EventNumber = 1 });
                scope.Complete();
            }

            Thread.Sleep(1.Seconds());

            // assert
            subReceivedEvents.ShouldBe(new[] { 1 }.ToList());
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers activateHandlers, IStoreSubscriptions storeSubscriptions)
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false);
            var messageQueue = new MsmqMessageQueue(inputQueueName).PurgeInputQueue();
            MsmqUtil.PurgeQueue("error");
            var serializer = new JsonMessageSerializer();
            var bus = new RebusBus(activateHandlers, messageQueue, messageQueue,
                                   storeSubscriptions, new InMemorySagaPersister(),
                                   this, serializer, pipelineInspector,
                                   new ErrorTracker("error"),
                                   null,
                                   new ConfigureAdditionalBehavior());

            EnsureProperDisposal(bus);
            EnsureProperDisposal(messageQueue);

            return bus;
        }

        protected void EnsureProperDisposal(IDisposable bus)
        {
            toDispose.Add(bus);
        }


        public string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(SomeEvent))
            {
                return PublisherInputQueueName;
            }

            return "";
        }

        class SomeEvent
        {
            public int EventNumber { get; set; }
        }
    }
}