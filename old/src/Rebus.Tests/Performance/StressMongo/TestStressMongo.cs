using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Castle.Windsor;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.MongoDb;
using Rebus.Serialization.Json;
using Rebus.Tests.Performance.StressMongo.Caf;
using Rebus.Tests.Performance.StressMongo.Caf.Messages;
using Rebus.Tests.Performance.StressMongo.Crm.Messages;
using Rebus.Tests.Performance.StressMongo.Dcc;
using Rebus.Tests.Performance.StressMongo.Factories;
using Rebus.Tests.Performance.StressMongo.Legal;
using Rebus.Tests.Performance.StressMongo.Legal.Messages;
using Rebus.Tests.Persistence;
using Rebus.Timeout;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Performance.StressMongo
{
    [Category(TestCategories.Mongo)]
    [Category(TestCategories.Integration)]
    [TestFixture(typeof(MsmqMessageQueueFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RabbitMqMessageQueueFactory), Category = TestCategories.Rabbit)]
    public class TestStressMongo<TFactory> : MongoDbFixtureBase, IDetermineMessageOwnership, IFlowLog where TFactory : IMessageQueueFactory, new()
    {
        const string CreditSagasCollectionName = "check_credit_sagas";
        const string LegalSagasCollectionName = "check_legal_sagas";
        const string CustomerInformationSagasCollectionName = "customer_information_sagas";
        const string TimeoutsCollectionName = "rebus.timeouts";
        const string SubscriptionsCollectionName = "rebus.subscriptions";

        readonly ConcurrentDictionary<string, ConcurrentQueue<string>> log = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

        readonly Dictionary<Type, string> endpointMappings =
            new Dictionary<Type, string>
                {
                    {typeof (CustomerCreated), GetEndpoint("crm")},
                    {typeof (CustomerCreditCheckComplete), GetEndpoint("caf")},
                    {typeof (CustomerLegallyApproved), GetEndpoint("legal")},
                };

        readonly List<IDisposable> stuffToDispose = new List<IDisposable>();

        IBus crm;
        IBus caf;
        IBus legal;
        IBus dcc;
        TimeoutService timeout;
        TFactory messageQueueFactory;

        protected override void DoSetUp()
        {
            messageQueueFactory = new TFactory();

            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false)
                {
                    MinLevel = LogLevel.Warn,
                    Filters = { l => true }
                };

            crm = CreateBus("crm", ContainerAdapterWith("crm"));
            caf = CreateBus("caf", ContainerAdapterWith("caf", typeof(CheckCreditSaga)));
            legal = CreateBus("legal", ContainerAdapterWith("legal", typeof(CheckSomeLegalStuffSaga)));
            dcc = CreateBus("dcc", ContainerAdapterWith("dcc", typeof(MaintainCustomerInformationSaga)));

            // clear saga data collections
            DropCollection(CreditSagasCollectionName);
            DropCollection(LegalSagasCollectionName);
            DropCollection(CustomerInformationSagasCollectionName);
            DropCollection(TimeoutsCollectionName);

            var timeoutServiceQueues = messageQueueFactory.GetQueue("rebus.timeout");
            timeout = new TimeoutService(new MongoDbTimeoutStorage(ConnectionString, TimeoutsCollectionName),
                                         timeoutServiceQueues.Item1,
                                         timeoutServiceQueues.Item2);
            timeout.Start();

            caf.Subscribe<CustomerCreated>();

            legal.Subscribe<CustomerCreated>();

            dcc.Subscribe<CustomerCreated>();
            dcc.Subscribe<CustomerCreditCheckComplete>();
            dcc.Subscribe<CustomerLegallyApproved>();

            Thread.Sleep(5.Seconds());
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100, Ignore = TestCategories.IgnoreLongRunningTests)]
        public void ItWorksWithSagasAndEverything(int count)
        {
            var no = 1;
            count.Times(() => crm.Publish(new CustomerCreated { Name = "John Doe" + no++, CustomerId = Guid.NewGuid() }));

            Thread.Sleep(15.Seconds() + (count * 0.8).Seconds());

            File.WriteAllText("stress-mongo.txt", FormatLogContents());

            var sagas = Collection<CustomerInformationSagaData>(CustomerInformationSagasCollectionName);
            var allSagas = sagas.FindAll();

            allSagas.Count().ShouldBe(count);
            allSagas.Count(s => s.CreditStatus.Complete).ShouldBe(count);
            allSagas.Count(s => s.LegalStatus.Complete).ShouldBe(count);
        }

        string FormatLogContents()
        {
            return string.Join(Environment.NewLine + Environment.NewLine, FormatLog(log));
        }

        static IEnumerable<string> FormatLog(ConcurrentDictionary<string, ConcurrentQueue<string>> log)
        {
            return log.Select(
                kvp =>
                string.Format(@"Log for {0}:
{1}", kvp.Key, FormatLog(kvp.Value)));
        }

        static string FormatLog(IEnumerable<string> value)
        {
            return string.Join(Environment.NewLine, value.Select(l => "    " + l));
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
            messageQueueFactory.CleanUp();
            timeout.Stop();
        }

        WindsorContainerAdapter ContainerAdapterWith(string serviceName, params Type[] types)
        {
            var container = new WindsorContainer();

            foreach (var type in types)
            {
                container.Register(Component.For(GetServices(type)).ImplementedBy(type).LifeStyle.Transient);
            }

            container.Register(Component.For<IFlowLog>().Instance(this));
            //container.Register(Component.For<IHandleMessages<object>>().Instance(new MessageLogger(this, serviceName)));

            return new WindsorContainerAdapter(container);
        }

        class MessageLogger : IHandleMessages<object>
        {
            readonly IFlowLog flowLog;
            readonly string id;

            public MessageLogger(IFlowLog flowLog, string id)
            {
                this.flowLog = flowLog;
                this.id = id;
            }

            public void Handle(object message)
            {
                flowLog.LogSequence(id, "Received {0}", FormatMessage(message));
            }

            string FormatMessage(object message)
            {
                var name = message.GetType().Name;

                return string.Format("{0}: {1}", name, GetInfo(message));
            }

            string GetInfo(object message)
            {
                if (message is SimulatedCreditCheckComplete)
                {
                    return ((SimulatedCreditCheckComplete)message).CustomerId.ToString();
                }

                if (message is CustomerCreated)
                {
                    var customerCreated = (CustomerCreated)message;

                    return string.Format("{0} {1}", customerCreated.CustomerId, customerCreated.Name);
                }

                if (message is TimeoutReply)
                {
                    var timeoutReply = (TimeoutReply)message;

                    return timeoutReply.CustomData;
                }

                if (message is CustomerCreditCheckComplete)
                {
                    return ((CustomerCreditCheckComplete)message).CustomerId.ToString();
                }

                if (message is SimulatedLegalCheckComplete)
                {
                    return ((SimulatedLegalCheckComplete)message).CustomerId.ToString();
                }

                if (message is CustomerLegallyApproved)
                {
                    return ((CustomerLegallyApproved)message).CustomerId.ToString();
                }

                return "n/a";
            }
        }

        Type[] GetServices(Type type)
        {
            return type.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            (i.GetGenericTypeDefinition() == typeof(IHandleMessages<>)
                             || i.GetGenericTypeDefinition() == typeof(IAmInitiatedBy<>)))
                .ToArray();
        }

        public string GetEndpointFor(Type messageType)
        {
            if (endpointMappings.ContainsKey(messageType))
                return endpointMappings[messageType];

            throw new ArgumentException(string.Format("Cannot determine owner of message type {0}", messageType));
        }

        IBus CreateBus(string serviceName, WindsorContainerAdapter containerAdapter)
        {
            DropCollection(SubscriptionsCollectionName);

            var inputQueueName = GetEndpoint(serviceName);

            var queue = messageQueueFactory.GetQueue(inputQueueName);

            var sagaPersister = new MongoDbSagaPersister(ConnectionString)
                .SetCollectionName<CheckCreditSagaData>(CreditSagasCollectionName)
                .SetCollectionName<CheckSomeLegalStuffSagaData>(LegalSagasCollectionName)
                .SetCollectionName<CustomerInformationSagaData>(CustomerInformationSagasCollectionName);

            var bus = new RebusBus(containerAdapter, queue.Item1, queue.Item2,
                                   new MongoDbSubscriptionStorage(ConnectionString, SubscriptionsCollectionName),
                                   sagaPersister, this,
                                   new JsonMessageSerializer(), new TrivialPipelineInspector(),
                                   new ErrorTracker("error"),
                                   null,
                                   new ConfigureAdditionalBehavior());

            stuffToDispose.Add(bus);

            containerAdapter.Container.Register(Component.For<IBus>().Instance(bus));

            return bus.Start(3);
        }

        static string GetEndpoint(string serviceName)
        {
            return "test_stress_mongo_" + serviceName;
        }

        public void LogFlow(Guid correlationId, string message, params object[] objs)
        {
            var key = correlationId.ToString();

            LogSequence(key, message, objs);
        }

        public void LogSequence(string id, string message, params object[] objs)
        {
            Log(id, message, objs);
        }

        void Log(string id, string message, object[] objs)
        {
            log.TryAdd(id, new ConcurrentQueue<string>());

            log[id].Enqueue(string.Format(message, objs));
        }
    }

    /// <summary>
    /// Logs stuff that's happened in relation to a given ID
    /// </summary>
    public interface IFlowLog
    {
        void LogFlow(Guid correlationId, string message, params object[] objs);
        void LogSequence(string id, string message, params object[] objs);
    }
}