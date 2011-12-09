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
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestWorker_MessageContext : FixtureBase, IHandleMessages<string>
    {
        Worker worker;
        IActivateHandlers activateHandlers;
        MessageReceiverForTesting receiveMessages;

        protected override void DoSetUp()
        {
            activateHandlers = Mock<IActivateHandlers>();
            receiveMessages = new MessageReceiverForTesting(new JsonMessageSerializer());
            worker = new Worker(new ErrorTracker(),
                                receiveMessages,
                                activateHandlers,
                                new InMemorySubscriptionStorage(),
                                new JsonMessageSerializer(),
                                new InMemorySagaPersister(),
                                new TrivialPipelineInspector());
        }

        [Test]
        public void MessageContextIsEstablishedWhenHandlerActivatorIsCalled()
        {
            // arrange
            worker.Start();
            var callWasIntercepted = false;

            activateHandlers.Stub(a => a.GetHandlerInstancesFor<string>())
                .WhenCalled(mi =>
                                {
                                    MessageContext.HasCurrent.ShouldBe(true);
                                    callWasIntercepted = true;
                                })
                .Return(new List<IHandleMessages<string>> { this });

            var message = new Message { Messages = new object[] { "w00t!" } };

            // act
            receiveMessages.Deliver(message);
            Thread.Sleep(300);

            // assert
            callWasIntercepted.ShouldBe(true);
        }

        public void Handle(string message)
        {
            Console.WriteLine("w00t!");
        }
    }
}