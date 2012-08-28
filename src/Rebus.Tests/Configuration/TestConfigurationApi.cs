using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Log4Net;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Serialization.Json;
using Rebus.Tests.Persistence;
using Rebus.Transports.Encrypted;
using Rebus.Transports.Msmq;
using Shouldly;
using Rebus.MongoDb;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestConfigurationApi : FixtureBase
    {
        [Test]
        public void CanConfigureEvents()
        {
            var adapter = new TestContainerAdapter();
            var raisedEvents = new List<string>();

            var configurer = Configure.With(adapter)
                .Events(e =>
                    {
                        e.BeforeTransportMessage += delegate { raisedEvents.Add("before transport message"); };
                        e.BeforeMessage += delegate { raisedEvents.Add("before message"); };
                        e.AfterMessage += delegate { raisedEvents.Add("after message"); };
                        e.AfterTransportMessage += delegate { raisedEvents.Add("after transport message"); };

                        e.MessageSent += delegate { raisedEvents.Add("message sent"); };
                        e.PoisonMessage += delegate { raisedEvents.Add("poison message"); };
                    })
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig());

            var bus = (IAdvancedBus)configurer.CreateBus();
            var events = ((RebusEvents) bus.Events);
            
            events.RaiseBeforeTransportMessage(null, null);
            events.RaiseBeforeMessage(null, null);
            events.RaiseAfterMessage(null, null, null);
            events.RaiseAfterTransportMessage(null, null, null);
            events.RaiseMessageSent(null, null, null);
            events.RaisePoisonMessage(null, null);

            raisedEvents.ShouldContain("before transport message");
            raisedEvents.ShouldContain("before message");
            raisedEvents.ShouldContain("after message");
            raisedEvents.ShouldContain("after transport message");
            raisedEvents.ShouldContain("message sent");
            raisedEvents.ShouldContain("poison message");
        }

        [Test]
        public void CanConfigureEncryption()
        {
            var configurer = Configure.With(new TestContainerAdapter())
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                .Decorators(d => d.EncryptMessageBodies());

            configurer.CreateBus();

            configurer.Backbone.SendMessages.ShouldBeTypeOf<RijndaelEncryptionTransportDecorator>();
            configurer.Backbone.ReceiveMessages.ShouldBeTypeOf<RijndaelEncryptionTransportDecorator>();
        }

        [Test]
        public void ConfiguringLoggingRegistersHandlerActivator()
        {
            var adapter = new TestContainerAdapter();

            var configurer = Configure.With(adapter)
                .Logging(l => l.None())
                .Serialization(s => s.UseJsonSerializer());

            configurer.Backbone.ActivateHandlers.ShouldNotBe(null);
        }

        [Test]
        public void NotConfiguringLoggingStillRegistersTheHandlerActivator()
        {
            var adapter = new TestContainerAdapter();

            var configurer = Configure.With(adapter)
                .Logging(l => l.None())
                .Serialization(s => s.UseJsonSerializer());

            configurer.Backbone.ActivateHandlers.ShouldNotBe(null);
        }

        [Test]
        public void CannotUseNullAsRebusLoggerFactory()
        {
            Assert.Throws<InvalidOperationException>(() => RebusLoggerFactory.Current = null);
        }

        [Test]
        public void CanConfigureLogging()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Logging(l => l.Console());

            RebusLoggerFactory.Current.ShouldBeTypeOf<ConsoleLoggerFactory>();
        }

        [Test]
        public void CanConfigureLog4NetLogging()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Logging(l => l.Log4Net());

            RebusLoggerFactory.Current.ShouldBeTypeOf<Log4NetLoggerFactory>();
        }

        [Test]
        public void CanConfigureColoredConsoleLogging()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Logging(l => l.ColoredConsole());

            RebusLoggerFactory.Current.ShouldBeTypeOf<ConsoleLoggerFactory>();
        }

        [Test]
        public void CanConfigureHandlerOrdering()
        {
            var adapter = new TestContainerAdapter();
            
            var configurer = Configure.With(adapter)
                .SpecifyOrderOfHandlers(h => h.First<SomeType>().Then<AnotherType>());

            configurer.Backbone.InspectHandlerPipeline.ShouldBeTypeOf<RearrangeHandlersPipelineInspector>();

            var inspector = (RearrangeHandlersPipelineInspector)configurer.Backbone.InspectHandlerPipeline;
            var order = inspector.GetOrder();
            order[0].ShouldBe(typeof(SomeType));
            order[1].ShouldBe(typeof(AnotherType));
        }

        [Test]
        public void CanConfigureMsmqTransport()
        {
            var adapter = new TestContainerAdapter();

            var configurer = Configure.With(adapter)
                .Transport(t => t.UseMsmq("some_input_queue", "some_error_queue"));

            configurer.Backbone.SendMessages.ShouldBeTypeOf<MsmqMessageQueue>();
            configurer.Backbone.ReceiveMessages.ShouldBeTypeOf<MsmqMessageQueue>();

            configurer.Backbone.SendMessages.ShouldBeSameAs(configurer.Backbone.ReceiveMessages);

            var msmqMessageQueue = (MsmqMessageQueue) configurer.Backbone.SendMessages;

            msmqMessageQueue.InputQueue.ShouldBe("some_input_queue");
        }

        [Test]
        public void CanConfigureMsmqTransportFromRebusConfigurationSection()
        {
            var adapter = new TestContainerAdapter();

            var configurer = Configure.With(adapter)
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig());

            configurer.Backbone.SendMessages.ShouldBeTypeOf<MsmqMessageQueue>();
            configurer.Backbone.ReceiveMessages.ShouldBeTypeOf<MsmqMessageQueue>();

            configurer.Backbone.SendMessages.ShouldBeSameAs(configurer.Backbone.ReceiveMessages);

            var msmqMessageQueue = (MsmqMessageQueue)configurer.Backbone.SendMessages;

            msmqMessageQueue.InputQueue.ShouldBe("this.is.my.input.queue");
        }

        [Test]
        public void CanConfigureSqlServerPersistence()
        {
            var adapter = new TestContainerAdapter();

            const string connectionstring = "connectionString";

            var configurer = Configure.With(adapter)
                .Sagas(s => s.StoreInSqlServer(connectionstring, "saga_table", "saga_index_table"))
                .Subscriptions(s => s.StoreInSqlServer(connectionstring, "subscriptions"));

            configurer.Backbone.StoreSagaData.ShouldBeTypeOf<SqlServerSagaPersister>();
            configurer.Backbone.StoreSubscriptions.ShouldBeTypeOf<SqlServerSubscriptionStorage>();


            var sagaPersister = (SqlServerSagaPersister)configurer.Backbone.StoreSagaData;
            sagaPersister.SagaTableName.ShouldBe("saga_table");
            sagaPersister.SagaIndexTableName.ShouldBe("saga_index_table");

            var subscriptionStorage = (SqlServerSubscriptionStorage)configurer.Backbone.StoreSubscriptions;
            subscriptionStorage.SubscriptionsTableName.ShouldBe("subscriptions");
        }

        [Test]
        public void CanConfigureJsonSerialization()
        {
            var adapter = new TestContainerAdapter();

            var configurer = Configure.With(adapter)
                .Serialization(s => s.UseJsonSerializer());

            configurer.Backbone.SerializeMessages.ShouldBeTypeOf<JsonMessageSerializer>();
        }

        [Test]
        public void CanConfigureCustomSerialization()
        {
            var adapter = new TestContainerAdapter();

            var serializer = Mock<ISerializeMessages>();
            
            var configurer = Configure.With(adapter)
                .Serialization(s => s.Use(serializer));

            configurer.Backbone.SerializeMessages.ShouldBe(serializer);
        }

        [Test]
        public void RequiresThatTransportIsConfigured()
        {
            var configurer = Configure.With(new TestContainerAdapter())
                .Sagas(s => s.StoreInSqlServer("connection", "siosjia", "jiogejigoe"))
                .Subscriptions(s => s.StoreInSqlServer("connection string", "jigeojge"))
                .Serialization(s => s.UseJsonSerializer());

            Assert.Throws<ConfigurationException>(() => configurer.CreateBus());
        }

        [Test]
        public void WhenTransportIsConfiguredEverythingElseWillDefaultToSomething()
        {
            var adapter = new TestContainerAdapter();

            var configurer = Configure.With(adapter)
                .Transport(t => t.UseMsmq("some_input_queue_name", "some_error_queue"))
                .DetermineEndpoints(d => d.FromNServiceBusConfiguration());

            configurer.CreateBus();

            configurer.Backbone.ActivateHandlers.ShouldNotBe(null);
            configurer.Backbone.StoreSagaData.ShouldNotBe(null);
            configurer.Backbone.StoreSubscriptions.ShouldNotBe(null);
            configurer.Backbone.InspectHandlerPipeline.ShouldNotBe(null);
            configurer.Backbone.SerializeMessages.ShouldNotBe(null);
            configurer.Backbone.DetermineDestination.ShouldNotBe(null);
        }

        [Test]
        public void CanConfigureAllTheMongoStuff()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Sagas(s => s.StoreInMongoDb(MongoDbFixtureBase.ConnectionString)
                                .SetCollectionName<FirstSagaData>("string_sagas")
                                .SetCollectionName<SecondSagaData>("datetime_sagas"));

        }

        class FirstSagaData: ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        class SecondSagaData: ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        public class TestContainerAdapter : IContainerAdapter
        {
            public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
            {
                throw new NotImplementedException();
            }

            public void Release(IEnumerable handlerInstances)
            {
                throw new NotImplementedException();
            }

            public void SaveBusInstances(IBus bus, IAdvancedBus advancedBus)
            {
                Bus = bus;
                AdvancedBus = advancedBus;
            }

            public IBus Bus { get; set; }

            public IAdvancedBus AdvancedBus { get; set; }
        }
    }

    public class SomeType
    {
    }
    public class AnotherType
    {
    }
}