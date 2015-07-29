using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Serialization.Json;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    internal class TestWorker_MessageContext : WorkerFixtureBase, IHandleMessages<string>
    {
        Worker worker;
        IActivateHandlers activateHandlers;
        MessageReceiverForTesting receiveMessages;

        protected override void DoSetUp()
        {
            activateHandlers = Mock<IActivateHandlers>();
            receiveMessages = new MessageReceiverForTesting(new JsonMessageSerializer());
            
            worker = CreateWorker(receiveMessages, activateHandlers);
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