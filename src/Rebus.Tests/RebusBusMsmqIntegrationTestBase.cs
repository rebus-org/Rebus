// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using System.Messaging;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Tests.Integration;
using Rebus.Transports.Msmq;
using log4net.Config;

namespace Rebus.Tests
{
    /// <summary>
    /// Test base class with helpers for running integration tests with
    /// <see cref="RebusBus"/> and <see cref="MsmqMessageQueue"/>.
    /// </summary>
    public abstract class RebusBusMsmqIntegrationTestBase : IDetermineDestination
    {
        static RebusBusMsmqIntegrationTestBase()
        {
            XmlConfigurator.Configure();
        }

        List<RebusBus> buses;
        
        protected JsonMessageSerializer serializer;

        [SetUp]
        public void SetUp()
        {
            buses = new List<RebusBus>();
        }

        [TearDown]
        public void TearDown()
        {
            buses.ForEach(b => b.Dispose());
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers activateHandlers)
        {
            var messageQueue = new MsmqMessageQueue(inputQueueName).PurgeInputQueue();
            serializer = new JsonMessageSerializer();
            var bus = new RebusBus(activateHandlers, messageQueue, messageQueue,
                                   new InMemorySubscriptionStorage(), new SagaDataPersisterForTesting(),
                                   this, serializer, new TrivialPipelineInspector());
            buses.Add(bus);
            return bus;
        }

        protected string PrivateQueueNamed(string queueName)
        {
            return string.Format(@".\private$\{0}", queueName);
        }

        public string GetEndpointFor(Type messageType)
        {
            throw new AssertionException(string.Format("Cannot route {0}", messageType));
        }

        protected static void EnsureQueueExists(string errorQueueName)
        {
            if (!MessageQueue.Exists(errorQueueName))
            {
                MessageQueue.Create(errorQueueName);
            }
        }
    }
}