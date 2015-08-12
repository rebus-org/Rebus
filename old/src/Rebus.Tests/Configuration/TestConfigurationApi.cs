using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Log4Net;
using Rebus.Logging;
using Rebus.Serilog;
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
        public void InvokingTheSameConfigurerTwiceYieldsAnException()
        {
            Assert.Throws<ConfigurationException>(() => Configure.With(new TestContainerAdapter())
                                                            .MessageOwnership(d => d.FromRebusConfigurationSection())
                                                            .MessageOwnership(d => d.FromRebusConfigurationSection()));

            Assert.Throws<ConfigurationException>(() => Configure.With(new TestContainerAdapter())
                                                                 .Timeouts(d => d.StoreInMemory())
                                                                 .Timeouts(d => d.StoreInMemory()));

            Assert.Throws<ConfigurationException>(() => Configure.With(new TestContainerAdapter())
                                                            .Transport(d => d.UseMsmqInOneWayClientMode())
                                                            .Transport(d => d.UseMsmqInOneWayClientMode()));

            Assert.Throws<ConfigurationException>(() => Configure.With(new TestContainerAdapter())
                                                            .Subscriptions(d => d.StoreInMemory())
                                                            .Subscriptions(d => d.StoreInMemory()));

            Assert.Throws<ConfigurationException>(() => Configure.With(new TestContainerAdapter())
                                                            .Sagas(d => d.StoreInMemory())
                                                            .Sagas(d => d.StoreInMemory()));

            Assert.Throws<ConfigurationException>(() => Configure.With(new TestContainerAdapter())
                                                            .Serialization(d => d.UseJsonSerializer())
                                                            .Serialization(d => d.UseJsonSerializer()));

            Assert.Throws<ConfigurationException>(() => Configure.With(new TestContainerAdapter())
                                                            .SpecifyOrderOfHandlers(d => d.First<string>())
                                                            .SpecifyOrderOfHandlers(d => d.First<string>()));

            Assert.DoesNotThrow(() => Configure.With(new TestContainerAdapter())
                                                            .Events(d => d.UncorrelatedMessage += (bus, message, saga) => { })
                                                            .Events(d => d.UncorrelatedMessage += (bus, message, saga) => { }));

            Assert.DoesNotThrow(() => Configure.With(new TestContainerAdapter())
                                                            .Decorators(d => d.AddDecoration(b => {}))
                                                            .Decorators(d => d.AddDecoration(b => {})));
        }

        [Test]
        public void CanConfigureTimeoutManager_External()
        {
            var adapter = new TestContainerAdapter();

            var configurer = Configure.With(adapter)
                                      .Timeouts(t => t.UseExternalTimeoutManager());

            configurer.Backbone.StoreTimeouts.ShouldBe(null);
        }

        [Test]
        public void CanConfigureTimeoutManager_InternalWithAppConfig()
        {
            var adapter = new TestContainerAdapter();

            var configurer = Configure.With(adapter)
                                      .Timeouts(t => t.StoreInSqlServer("someConnectionString", "timeouts"));

            configurer.Backbone.StoreTimeouts.ShouldBeOfType<SqlServerTimeoutStorage>();
        }

        [Test]
        public void CanConfigureEvents()
        {
            var adapter = new TestContainerAdapter();
            var raisedEvents = new List<string>();
            var unitOfWorkManagerInstanceThatCanBeRecognized = Mock<IUnitOfWorkManager>();

            var configurer = Configure.With(adapter)
                .Events(e =>
                    {
                        e.BusStarted += delegate { raisedEvents.Add("bus started"); };

                        e.BeforeTransportMessage += delegate { raisedEvents.Add("before transport message"); };
                        e.BeforeMessage += delegate { raisedEvents.Add("before message"); };
                        e.AfterMessage += delegate { raisedEvents.Add("after message"); };
                        e.AfterTransportMessage += delegate { raisedEvents.Add("after transport message"); };

                        e.MessageSent += delegate { raisedEvents.Add("message sent"); };
                        e.PoisonMessage += delegate { raisedEvents.Add("poison message"); };

                        e.UncorrelatedMessage += delegate { raisedEvents.Add("uncorrelated message"); };

                        e.MessageContextEstablished += delegate { raisedEvents.Add("message context established"); };
                        e.AddUnitOfWorkManager(unitOfWorkManagerInstanceThatCanBeRecognized);

                        e.BusStopped += delegate { raisedEvents.Add("bus stopped"); };
                    })
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig());

            var bus = (IBus)configurer.CreateBus();
            var events = (RebusEvents) bus.Advanced.Events;
            
            events.RaiseBusStarted(null);
            events.RaiseBeforeTransportMessage(null, null);
            events.RaiseBeforeMessage(null, null);
            events.RaiseAfterMessage(null, null, null);
            events.RaiseAfterTransportMessage(null, null, null);
            events.RaiseMessageSent(null, null, null);
            events.RaisePoisonMessage(null, null, null);
            events.RaiseUncorrelatedMessage(null, null, null);
            events.RaiseMessageContextEstablished(null, null);
            events.RaiseBusStopped(null);

            raisedEvents.ShouldContain("bus started");
            raisedEvents.ShouldContain("before transport message");
            raisedEvents.ShouldContain("before message");
            raisedEvents.ShouldContain("after message");
            raisedEvents.ShouldContain("after transport message");
            raisedEvents.ShouldContain("message sent");
            raisedEvents.ShouldContain("poison message");
            raisedEvents.ShouldContain("uncorrelated message");
            raisedEvents.ShouldContain("message context established");
            raisedEvents.ShouldContain("bus stopped");

            events.UnitOfWorkManagers.ShouldContain(unitOfWorkManagerInstanceThatCanBeRecognized);
        }

        [Test]
        public void CanConfigureEncryption()
        {
            var configurer = Configure.With(new TestContainerAdapter())
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                .Decorators(d => d.EncryptMessageBodies());

            configurer.CreateBus();

            configurer.Backbone.SendMessages.ShouldBeOfType<EncryptionAndCompressionTransportDecorator>();
            configurer.Backbone.ReceiveMessages.ShouldBeOfType<EncryptionAndCompressionTransportDecorator>();
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

            RebusLoggerFactory.Current.ShouldBeOfType<ConsoleLoggerFactory>();
        }

        [Test]
        public void CanConfigureLog4NetLogging()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Logging(l => l.Log4Net());

            RebusLoggerFactory.Current.ShouldBeOfType<Log4NetLoggerFactory>();
        }

        [Test]
        public void CanConfigureSerilogLogging()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Logging(l => l.Serilog());

            RebusLoggerFactory.Current.ShouldBeOfType<SerilogLoggerFactory>();
        }

        [Test]
        public void CanConfigureColoredConsoleLogging()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Logging(l => l.ColoredConsole());

            RebusLoggerFactory.Current.ShouldBeOfType<ConsoleLoggerFactory>();
        }

        [Test]
        public void CanConfigureHandlerOrdering()
        {
            var adapter = new TestContainerAdapter();
            
            var configurer = Configure.With(adapter)
                .SpecifyOrderOfHandlers(h => h.First<SomeType>().Then<AnotherType>());

            configurer.Backbone.InspectHandlerPipeline.ShouldBeOfType<RearrangeHandlersPipelineInspector>();

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

            configurer.Backbone.SendMessages.ShouldBeOfType<MsmqMessageQueue>();
            configurer.Backbone.ReceiveMessages.ShouldBeOfType<MsmqMessageQueue>();

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

            configurer.Backbone.SendMessages.ShouldBeOfType<MsmqMessageQueue>();
            configurer.Backbone.ReceiveMessages.ShouldBeOfType<MsmqMessageQueue>();

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

            configurer.Backbone.StoreSagaData.ShouldBeOfType<SqlServerSagaPersister>();
            configurer.Backbone.StoreSubscriptions.ShouldBeOfType<SqlServerSubscriptionStorage>();


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
                .Serialization(s => s.UseJsonSerializer()
                                        .AddNameResolver(t => null)
                                        .AddTypeResolver(d => null));

            configurer.Backbone.SerializeMessages.ShouldBeOfType<JsonMessageSerializer>();
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
                .Transport(t => t.UseMsmq("some_input_queue_name", "some_error_queue"));

            configurer.CreateBus();

            configurer.Backbone.ActivateHandlers.ShouldNotBe(null);
            configurer.Backbone.StoreSagaData.ShouldNotBe(null);
            configurer.Backbone.StoreSubscriptions.ShouldNotBe(null);
            configurer.Backbone.InspectHandlerPipeline.ShouldNotBe(null);
            configurer.Backbone.SerializeMessages.ShouldNotBe(null);
            configurer.Backbone.DetermineMessageOwnership.ShouldNotBe(null);
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

        [Test]
        public void CanConfigureEndpointMapperWithFilter()
        {
            var adapter = new TestContainerAdapter();

            var backboneWithoutFilter =
                (DetermineMessageOwnershipFromRebusConfigurationSection)
                Configure.With(adapter)
                    .MessageOwnership(d => d.FromRebusConfigurationSection())
                    .Backbone.DetermineMessageOwnership;

            var backboneWithFilter =
                (DetermineMessageOwnershipFromRebusConfigurationSection)
                Configure.With(adapter)
                    .MessageOwnership(d => d.FromRebusConfigurationSectionWithFilter(f => f == typeof(SomeType)))
                    .Backbone.DetermineMessageOwnership;

            Assert.DoesNotThrow(() => backboneWithoutFilter.GetEndpointFor(typeof (SomeType)));

            Assert.DoesNotThrow(() => backboneWithFilter.GetEndpointFor(typeof(SomeType)));
            Assert.Throws<InvalidOperationException>(() => backboneWithFilter.GetEndpointFor(typeof (AnotherType)));
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
            public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
            {
                throw new NotImplementedException();
            }

            public void Release(IEnumerable handlerInstances)
            {
                throw new NotImplementedException();
            }

            public void SaveBusInstances(IBus bus)
            {
                Bus = bus;
            }

            public IBus Bus { get; set; }
        }
    }

    public class SomeType
    {
    }
    public class AnotherType
    {
    }
}