using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Persistence.SqlServer;
using Rebus.Shared;
using Rebus.Tests.Persistence;
using Rebus.Transports.Msmq;
using Rebus.Transports.Sql;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Category(TestCategories.MsSql)]
    public class TestUsingTransactionScope : SqlServerFixtureBase
    {
        const string PublisherInputQueueName = "test.sub-unsub.publisher";
        const string SubscriberInputQueueName = "test.sub-unsub.subscriber";
        const string SubscriptionsTableName = "subscriptions";

        List<IDisposable> toDispose;

        protected override void DoSetUp()
        {
            toDispose = new List<IDisposable>();
            DropTable(SubscriptionsTableName);
        }

        protected override void DoTearDown()
        {
            toDispose.ForEach(b => b.Dispose());
            toDispose.Clear();
        }

        [Test]
        public void CanPublishWithinTransactionScopeWhenProvidingTransactionLessConnectionHolder()
        {
            // arrange
            var subscriptionStorage = new SqlServerSubscriptionStorage(() =>
            {
                var sqlConnection = new SqlConnection(ConnectionString);
                sqlConnection.Open();
                return ConnectionHolder.ForNonTransactionalWork(sqlConnection);
            }, SubscriptionsTableName);

            subscriptionStorage.EnsureTableIsCreated();

            var publisher = CreateBus(PublisherInputQueueName, subscriptionStorage);

            var subReceivedEvents = new List<int>();

            var sub = CreateBus(SubscriberInputQueueName, subscriptionStorage)
                .Handle<SomeEvent>(e => subReceivedEvents.Add(e.EventNumber));

            sub.Bus.Subscribe<SomeEvent>();

            // act
            Thread.Sleep(1.Seconds());

            using (var scope = new TransactionScope())
            {
                publisher.Bus.Publish(new SomeEvent { EventNumber = 1 });
                
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
            var subscriptionStorage = new SqlServerSubscriptionStorage(ConnectionString, SubscriptionsTableName);

            subscriptionStorage.EnsureTableIsCreated();

            var publisher = CreateBus(PublisherInputQueueName, subscriptionStorage);

            var subReceivedEvents = new List<int>();

            var sub = CreateBus(SubscriberInputQueueName, subscriptionStorage)
                .Handle<SomeEvent>(e => subReceivedEvents.Add(e.EventNumber));

            sub.Bus.Subscribe<SomeEvent>();

            // act
            Thread.Sleep(1.Seconds());

            using (var scope = new TransactionScope())
            {
                publisher.Bus.Publish(new SomeEvent { EventNumber = 1 });
                
                scope.Complete();
            }

            Thread.Sleep(1.Seconds());

            // assert
            subReceivedEvents.ShouldBe(new[] { 1 }.ToList());
        }

        protected BuiltinContainerAdapter CreateBus(string inputQueueName, IStoreSubscriptions subscriptionStorage)
        {
            MsmqUtil.PurgeQueue(inputQueueName);
            MsmqUtil.PurgeQueue("error");

            var adapter = new BuiltinContainerAdapter();
            
            EnsureProperDisposal(adapter);
            
            Configure.With(adapter)
                .Transport(t => t.UseMsmq(inputQueueName, "error"))
                .MessageOwnership(o => o.Use(this))
                .CreateBus()
                .Start(1);

            return adapter;
        }

        protected void EnsureProperDisposal(IDisposable bus)
        {
            toDispose.Add(bus);
        }


        public override string GetEndpointFor(Type messageType)
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