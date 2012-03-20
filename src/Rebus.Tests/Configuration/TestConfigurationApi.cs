using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Configuration.Configurers;
using Rebus.Log4Net;
using Rebus.Logging;
using Rebus.Persistence.SqlServer;
using Rebus.Serialization.Json;
using Rebus.Transports.Encrypted;
using Rebus.Transports.Msmq;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestConfigurationApi : FixtureBase
    {
        [Test]
        public void CanConfigureDiscovery()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .DetermineEndpoints(d => d.FromRebusConfigurationSection())
                .Discovery(d =>
                               {
                                   d.Handlers.LoadFrom(Assembly.GetExecutingAssembly());
                               });
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

            Configure.With(adapter)
                .SpecifyOrderOfHandlers(h => h.First<SomeType>().Then<AnotherType>());

            var registration = adapter.Registrations.Single(r => r.Instance.GetType() == typeof(RearrangeHandlersPipelineInspector));

            var inspector = (RearrangeHandlersPipelineInspector)registration.Instance;
            var order = inspector.GetOrder();
            order[0].ShouldBe(typeof(SomeType));
            order[1].ShouldBe(typeof(AnotherType));
        }

        [Test]
        public void CanConfigureMsmqTransport()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Transport(t => t.UseMsmq("some_input_queue", "some_error_queue"));

            var registrations = adapter.Registrations
                .Where(r => r.Instance.GetType() == typeof (MsmqMessageQueue))
                .ToList();

            registrations.Count.ShouldBe(2);

            var msmqMessageQueue = (MsmqMessageQueue)registrations.First().Instance;
            msmqMessageQueue.InputQueue.ShouldBe(@"some_input_queue");
            msmqMessageQueue.ErrorQueueName.ShouldBe(@"some_error_queue");
        }
        
        [Test]
        public void CanConfigureMsmqTransportFromRebusConfigurationSection()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig());

            var registrations = adapter.Registrations
                .Where(r => r.Instance.GetType() == typeof (MsmqMessageQueue))
                .ToList();

            registrations.Count.ShouldBe(2);

            var msmqMessageQueue = (MsmqMessageQueue)registrations.First().Instance;
            msmqMessageQueue.InputQueue.ShouldBe(@"this.is.my.input.queue");
        }

        [Test]
        public void CanConfigureSqlServerPersistence()
        {
            var adapter = new TestContainerAdapter();

            const string connectionstring = "connectionString";

            Configure.With(adapter)
                .Sagas(s => s.StoreInSqlServer(connectionstring, "saga_table", "saga_index_table"))
                .Subscriptions(s => s.StoreInSqlServer(connectionstring, "subscriptions"));

            var sagaRegistration = adapter.Registrations.Single(r => r.Instance.GetType() == typeof(SqlServerSagaPersister));
            var subRegistration = adapter.Registrations.Single(r => r.Instance.GetType() == typeof (SqlServerSubscriptionStorage));
            
            var sagaPersister = (SqlServerSagaPersister)sagaRegistration.Instance;
            sagaPersister.SagaTableName.ShouldBe("saga_table");
            sagaPersister.SagaIndexTableName.ShouldBe("saga_index_table");
            
            var subscriptionStorage = (SqlServerSubscriptionStorage)subRegistration.Instance;
            subscriptionStorage.SubscriptionsTableName.ShouldBe("subscriptions");
        }

        [Test]
        public void CanConfigureJsonSerialization()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Serialization(s => s.UseJsonSerializer());

            var registration = adapter.Registrations.Single(r => r.Instance.GetType() == typeof(JsonMessageSerializer));
            var serializer = (JsonMessageSerializer)registration.Instance;
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

            Configure.With(adapter)
                .Transport(t => t.UseMsmq("some_input_queue_name", "some_error_queue"))
                .DetermineEndpoints(d => d.FromNServiceBusConfiguration())
                .CreateBus();

            adapter.HasImplementationOf(typeof (IActivateHandlers)).ShouldBe(true);
            adapter.HasImplementationOf(typeof (IStoreSubscriptions)).ShouldBe(true);
            adapter.HasImplementationOf(typeof (IStoreSagaData)).ShouldBe(true);
            adapter.HasImplementationOf(typeof (IInspectHandlerPipeline)).ShouldBe(true);
            adapter.HasImplementationOf(typeof (ISerializeMessages)).ShouldBe(true);
        }

        [Test]
        public void CanConfigureEncryptedMsmqTransport()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Transport(t => t.UseEncryptedMsmqAndGetConfigurationFromAppConfig());

            adapter.Resolve<ISendMessages>().ShouldBeTypeOf<RijndaelEncryptionTransportDecorator>();
            adapter.Resolve<IReceiveMessages>().ShouldBeTypeOf<RijndaelEncryptionTransportDecorator>();
        }

        /// <summary>
        /// Look ma! - an IoC container.... :)
        /// </summary>
        class TestContainerAdapter : IContainerAdapter
        {
            interface IResolver
            {
                object Get();
            }
            class InstanceResolver :IResolver
            {
                readonly object instance;
                public InstanceResolver(object instance)
                {
                    this.instance = instance;
                }

                public object Get()
                {
                    return instance;
                }
            }
            class RecursiveTypeMappingResolver : IResolver
            {
                readonly Type implementationType;
                readonly IContainerAdapter container;
                readonly bool singleton;
                object cachedInstance;

                public RecursiveTypeMappingResolver(Type implementationType, IContainerAdapter container, bool singleton)
                {
                    this.implementationType = implementationType;
                    this.container = container;
                    this.singleton = singleton;
                }

                public object Get()
                {
                    if (singleton && cachedInstance != null) return cachedInstance;

                    try
                    {
                        var constructor = implementationType.GetConstructors().First();
                        var parameters = constructor.GetParameters()
                            .Select(p => container.GetType()
                                             .GetMethod("Resolve")
                                             .MakeGenericMethod(p.ParameterType).Invoke(container, new object[0]))
                            .ToArray();

                        cachedInstance = Activator.CreateInstance(implementationType, parameters);

                        return cachedInstance;
                    }
                    catch(Exception e)
                    {
                        throw new ApplicationException(string.Format("Could not resolve {0}", implementationType), e);
                    }
                }
            }

            readonly Dictionary<Type, List<IResolver>> resolvers = new Dictionary<Type, List<IResolver>>();
            readonly List<Registration> registrations = new List<Registration>();

            public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
            {
                throw new NotImplementedException();
            }

            public void Release(IEnumerable handlerInstances)
            {
                throw new NotImplementedException();
            }

            public void RegisterInstance(object instance, params Type[] serviceTypes)
            {
                registrations.Add(new Registration(instance, serviceTypes));
                
                foreach(var type in serviceTypes)
                {
                    AddResolver(type, new InstanceResolver(instance));
                }
            }

            public List<Registration> Registrations
            {
                get { return registrations; }
            }

            public void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
            {
                foreach (var type in serviceTypes)
                {
                    AddResolver(type, new RecursiveTypeMappingResolver(implementationType, this, lifestyle == Lifestyle.Singleton));
                }
            }

            void AddResolver(Type type, IResolver resolver)
            {
                if (!resolvers.ContainsKey(type))
                    resolvers[type] = new List<IResolver>();

                resolvers[type].Add(resolver);
            }

            public bool HasImplementationOf(Type serviceType)
            {
                return resolvers.ContainsKey(serviceType);
            }

            public TService Resolve<TService>()
            {
                return (TService) resolvers[typeof (TService)].First().Get();
            }

            public TService[] ResolveAll<TService>()
            {
                throw new NotImplementedException();
            }

            public void Release(object obj)
            {
                throw new NotImplementedException();
            }
        }

        class Registration
        {
            readonly object instance;
            readonly Type[] serviceTypes;

            public Registration(object instance, Type[] serviceTypes)
            {
                this.instance = instance;
                this.serviceTypes = serviceTypes;
            }

            public object Instance
            {
                get { return instance; }
            }

            public Type[] ServiceTypes
            {
                get { return serviceTypes; }
            }
        }
    }

    public class SomeType
    {
    }
    public class AnotherType
    {
    }
}