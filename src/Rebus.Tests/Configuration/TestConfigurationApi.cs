using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.SqlServer;
using Rebus.Serialization.Json;
using Rebus.Transports.Msmq;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestConfigurationApi : FixtureBase
    {
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
                .Transport(t => t.UseMsmq("some_input_queue"));

            var registrations = adapter.Registrations
                .Where(r => r.Instance.GetType() == typeof (MsmqMessageQueue))
                .ToList();

            registrations.Count.ShouldBe(2);

            var msmqMessageQueue = (MsmqMessageQueue)registrations.First().Instance;
            msmqMessageQueue.InputQueue.ShouldBe(@".\private$\some_input_queue");
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
                .Transport(t => t.UseMsmq("some_input_queue_name"))
                .DetermineEndpoints(d => d.FromNServiceBusConfiguration())
                .CreateBus();

            adapter.HasImplementationOf(typeof (IActivateHandlers)).ShouldBe(true);
            adapter.HasImplementationOf(typeof (IStoreSubscriptions)).ShouldBe(true);
            adapter.HasImplementationOf(typeof (IStoreSagaData)).ShouldBe(true);
            adapter.HasImplementationOf(typeof (IInspectHandlerPipeline)).ShouldBe(true);
            adapter.HasImplementationOf(typeof (ISerializeMessages)).ShouldBe(true);
        }

        class TestContainerAdapter : IContainerAdapter
        {
            readonly List<Registration> registrations = new List<Registration>();

            public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
            {
                throw new NotImplementedException();
            }

            public void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances)
            {
            }

            public void RegisterInstance(object instance, params Type[] serviceTypes)
            {
                registrations.Add(new Registration(instance, serviceTypes));
            }

            public bool HasImplementationOf(Type serviceType)
            {
                return registrations.Any(r => r.ServiceTypes.Any(t => t == serviceType));
            }

            public IStartableBus GetStartableBus()
            {
                var constructorInfo = typeof (RebusBus).GetConstructors().First();
                
                var ctorParameters = constructorInfo.GetParameters()
                    .Select(p => Resolve(p.ParameterType))
                    .ToArray();
                
                var obj = constructorInfo.Invoke(BindingFlags.Public, null, ctorParameters, null);
                
                return (IStartableBus) obj;
            }

            object Resolve(Type typeToResolve)
            {
                var firstOrDefault = registrations
                    .FirstOrDefault(r => r.ServiceTypes.Any(t => t == typeToResolve));

                if (firstOrDefault == null)
                {
                    throw new ConfigurationException("No type registered as an implementation of {0}", typeToResolve);
                }

                return firstOrDefault.Instance;
            }

            public List<Registration> Registrations
            {
                get { return registrations; }
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