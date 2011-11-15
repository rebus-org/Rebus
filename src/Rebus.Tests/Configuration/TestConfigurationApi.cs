using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Persistence.SqlServer;
using Rebus.Transports.Msmq;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestConfigurationApi : FixtureBase
    {
        [Test]
        public void CanConfigureMsmqTransport()
        {
            var adapter = new TestContainerAdapter();

            Configure.With(adapter)
                .Transport(t => t.UseMsmq("some_input_queue"));

            adapter.Registrations.Count.ShouldBe(1);
            
            var registration = adapter.Registrations.Single(r => r.Instance.GetType() == typeof(MsmqMessageQueue));
            
            var msmqMessageQueue = (MsmqMessageQueue)registration.Instance;
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

            adapter.Registrations.Count.ShouldBe(2);
            
            var sagaRegistration = adapter.Registrations.Single(r => r.Instance.GetType() == typeof(SqlServerSagaPersister));
            var subRegistration = adapter.Registrations.Single(r => r.Instance.GetType() == typeof (SqlServerSubscriptionStorage));
            
            var sagaPersister = (SqlServerSagaPersister)sagaRegistration.Instance;
            sagaPersister.SagaTableName.ShouldBe("saga_table");
            sagaPersister.SagaIndexTableName.ShouldBe("saga_index_table");
            
            var subscriptionStorage = (SqlServerSubscriptionStorage)subRegistration.Instance;
            subscriptionStorage.SubscriptionsTableName.ShouldBe("subscriptions");
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
}