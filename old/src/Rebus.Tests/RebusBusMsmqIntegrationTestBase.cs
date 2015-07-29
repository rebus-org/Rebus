using System;
using System.Collections.Generic;
using System.Messaging;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Shared;
using Rebus.Tests.Integration;
using Rebus.Transports.Msmq;
using log4net.Config;

namespace Rebus.Tests
{
    /// <summary>
    /// Test base class with helpers for running integration tests with
    /// <see cref="RebusBus"/> and <see cref="MsmqMessageQueue"/>.
    /// </summary>
    public abstract class RebusBusMsmqIntegrationTestBase : IDetermineMessageOwnership
    {
        const string ErrorQueueName = "error";

        static RebusBusMsmqIntegrationTestBase()
        {
            XmlConfigurator.Configure();
        }

        List<IDisposable> toDispose;
        
        protected JsonMessageSerializer serializer;
        protected RearrangeHandlersPipelineInspector pipelineInspector = new RearrangeHandlersPipelineInspector();

        [SetUp]
        public void SetUp()
        {
            TimeMachine.Reset();

            toDispose = new List<IDisposable>();

            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
            MsmqUtil.Delete(ErrorQueueName);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                DoTearDown();
            }
            finally
            {
                toDispose.ForEach(b => b.Dispose());
            }

            MsmqUtil.Delete(ErrorQueueName);
        }

        protected virtual void DoTearDown()
        {
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers activateHandlers)
        {
            return CreateBus(inputQueueName, activateHandlers,
                             new InMemorySubscriptionStorage(),
                             new SagaDataPersisterForTesting(),
                             ErrorQueueName);
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers activateHandlers, IStoreSubscriptions storeSubscriptions, IStoreSagaData storeSagaData, string errorQueueName)
        {
            var messageQueue = new MsmqMessageQueue(inputQueueName).PurgeInputQueue();
            MsmqUtil.PurgeQueue(errorQueueName);
            serializer = new JsonMessageSerializer();
            var bus = new RebusBus(activateHandlers, messageQueue, messageQueue,
                                   storeSubscriptions, storeSagaData,
                                   this, serializer, pipelineInspector,
                                   new ErrorTracker(errorQueueName),
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

        protected string PrivateQueueNamed(string queueName)
        {
            return string.Format(@".\private$\{0}", queueName);
        }

        public virtual string GetEndpointFor(Type messageType)
        {
            throw new AssertionException(string.Format("Cannot route {0}", messageType));
        }

        protected static void EnsureQueueExists(string errorQueueName)
        {
            if (!MessageQueue.Exists(errorQueueName))
            {
                MessageQueue.Create(errorQueueName, transactional: true);
            }
        }
    }
}