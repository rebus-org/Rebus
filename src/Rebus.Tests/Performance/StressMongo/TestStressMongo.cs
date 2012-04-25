using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Castle.Windsor;
using Rebus.MongoDb;
using Rebus.Serialization.Json;
using Rebus.Tests.Performance.StressMongo.Caf;
using Rebus.Tests.Performance.StressMongo.Crm;
using Rebus.Tests.Performance.StressMongo.Legal;
using Rebus.Tests.Persistence.MongoDb;
using Rebus.Timeout;
using Rebus.Transports.Msmq;
using System.Linq;

namespace Rebus.Tests.Performance.StressMongo
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestStressMongo : MongoDbFixtureBase, IDetermineDestination, IFlowLog
    {
        readonly Dictionary<Guid, List<string>> log = new Dictionary<Guid, List<string>>();
        
        readonly Dictionary<Type, string> endpointMappings =
            new Dictionary<Type, string>
                {
                    {typeof (CustomerCreated), GetEndpoint("crm")},
                };

        readonly List<IDisposable> stuffToDispose = new List<IDisposable>();
        
        IBus crm;
        IBus caf;
        IBus legal;
        IBus dcc;
        TimeoutService timeout;

        protected override void DoSetUp()
        {
            crm = CreateBus("crm", ContainerAdapterWith());
            caf = CreateBus("caf", ContainerAdapterWith(typeof(CheckCreditSaga)));
            legal = CreateBus("legal", ContainerAdapterWith(typeof(CheckSomeLegalStuffSaga)));
            dcc = CreateBus("dcc", ContainerAdapterWith());

            timeout = new TimeoutService(new MongoDbTimeoutStorage(ConnectionString, "rebus.timeouts"));
            timeout.Start();

            caf.Subscribe<CustomerCreated>();

            Thread.Sleep(1.Seconds());
        }

        [Test]
        public void StatementOfSomething()
        {
            crm.Publish(new CustomerCreated{Name = "John Doe", CustomerId = Guid.NewGuid()});
            crm.Publish(new CustomerCreated{Name = "Jane Doe", CustomerId = Guid.NewGuid()});
            crm.Publish(new CustomerCreated{Name = "Moe Doe", CustomerId = Guid.NewGuid()});

            Thread.Sleep(20.Seconds());

            File.WriteAllText("stress-mongo.txt", FormatLogContents());
        }

        string FormatLogContents()
        {
            return string.Join(Environment.NewLine + Environment.NewLine,
                               log.Select(
                                   kvp =>
                                   string.Format(@"Log for {0}:
{1}", kvp.Key,
                                                 string.Join(Environment.NewLine,
                                                             kvp.Value.Select(l => string.Format("    " + l))))));
        }

        protected override void DoTearDown()
        {
            stuffToDispose.ForEach(s =>
                                       {
                                           try
                                           {
                                               s.Dispose();
                                           }
                                           catch
                                           {
                                           }
                                       });

            timeout.Stop();
        }

        IContainerAdapter ContainerAdapterWith(params Type[] types)
        {
            var container = new WindsorContainer();

            foreach (var type in types)
            {
                container.Register(Component.For(GetServices(type)).ImplementedBy(type).LifeStyle.Transient);
            }

            container.Register(Component.For<IFlowLog>().Instance(this));

            return new WindsorContainerAdapter(container);
        }

        Type[] GetServices(Type type)
        {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof (IHandleMessages<>)
                             || i.GetGenericTypeDefinition() == typeof (IAmInitiatedBy<>)))
                .ToArray();
        }

        public string GetEndpointFor(Type messageType)
        {
            if (endpointMappings.ContainsKey(messageType))
                return endpointMappings[messageType];

            throw new ArgumentException(string.Format("Cannot determine owner of message type {0}", messageType));
        }

        IBus CreateBus(string serviceName, IContainerAdapter containerAdapter)
        {
            var msmqMessageQueue = new MsmqMessageQueue(GetEndpoint(serviceName), "error");
            var bus = new RebusBus(containerAdapter, msmqMessageQueue, msmqMessageQueue,
                                   new MongoDbSubscriptionStorage(ConnectionString, "rebus.subscriptions"),
                                   new MongoDbSagaPersister(ConnectionString, serviceName + ".sagas"), this,
                                   new JsonMessageSerializer(), new TrivialPipelineInspector());

            stuffToDispose.Add(bus);

            containerAdapter.RegisterInstance(bus, typeof(IBus));

            return bus.Start(1);
        }

        static string GetEndpoint(string serviceName)
        {
            return "test.stress.mongo." + serviceName;
        }

        public void Log(Guid correlationId, string message, params object[] objs)
        {
            if (!log.ContainsKey(correlationId))
            {
                log[correlationId] = new List<string>();
            }

            log[correlationId].Add(string.Format(message, objs));
        }
    }

    /// <summary>
    /// Logs stuff that's happened in relation to a given ID
    /// </summary>
    public interface IFlowLog
    {
        void Log(Guid correlationId, string message, params object[] objs);
    }
}