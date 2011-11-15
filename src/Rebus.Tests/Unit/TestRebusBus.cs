using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Tests.Integration;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestRebusBus : FixtureBase
    {
        RebusBus bus;
        IReceiveMessages receiveMessages;
        IActivateHandlers activateHandlers;
        IDetermineDestination determineDestination;
        ISendMessages sendMessages;
        ISerializeMessages serializeMessages;
        IStoreSagaData storeSagaData;
        IInspectHandlerPipeline inspectHandlerPipeline;

        protected override void DoSetUp()
        {
            receiveMessages = Mock<IReceiveMessages>();
            activateHandlers = Mock<IActivateHandlers>();
            determineDestination = Mock<IDetermineDestination>();
            sendMessages = Mock<ISendMessages>();
            serializeMessages = Mock<ISerializeMessages>();
            storeSagaData = Mock<IStoreSagaData>();
            inspectHandlerPipeline = new TrivialPipelineInspector();
            bus = new RebusBus(activateHandlers,
                               sendMessages,
                               receiveMessages,
                               Mock<IStoreSubscriptions>(),
                               storeSagaData,
                               determineDestination, serializeMessages, inspectHandlerPipeline);
        }

        protected override void DoTearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void SendsMessagesToTheRightDestination()
        {
            // arrange
            determineDestination.Stub(d => d.GetEndpointFor(typeof(PolymorphicMessage))).Return("woolala");
            var theMessageThatWasSent = new PolymorphicMessage();

            var someTransportMessage = new TransportMessage();
            serializeMessages.Stub(s => s.Serialize(Arg<Message>.Matches(t => t.Messages[0] == theMessageThatWasSent)))
                .Return(someTransportMessage);

            // act
            bus.Send(theMessageThatWasSent);

            // assert
            sendMessages.AssertWasCalled(s => s.Send("woolala", someTransportMessage));
        }

        [Test]
        public void SubscribesToMessagesFromTheRightPublisher()
        {
            // arrange
            determineDestination.Stub(d => d.GetEndpointFor(typeof(PolymorphicMessage))).Return("woolala");
            receiveMessages.Stub(r => r.InputQueue).Return("my input queue");

            var someTransportMessage = new TransportMessage();
            serializeMessages
                .Stub(s => s.Serialize(Arg<Message>.Matches(t => t.Headers[Headers.ReturnAddress] == "my input queue" &&
                                                                 ((SubscriptionMessage)t.Messages[0]).Type ==
                                                                 typeof(PolymorphicMessage).FullName)))
                .Return(someTransportMessage);

            // act
            bus.Subscribe<PolymorphicMessage>();

            // assert
            sendMessages.AssertWasCalled(s => s.Send("woolala", someTransportMessage));
        }

        [Test]
        public void CanDoPolymorphicMessageDispatch()
        {
            var someTransportMessage = new TransportMessage { Id = "some id" };
            receiveMessages.Stub(r => r.ReceiveMessage()).Return(someTransportMessage);

            serializeMessages.Stub(s => s.Deserialize(someTransportMessage))
                .Return(new Message
                            {
                                Messages = new object[]
                                               {
                                                   new PolymorphicMessage()
                                               }
                            });

            var manualResetEvent = new ManualResetEvent(false);

            var handler = new SomeHandler(manualResetEvent);

            activateHandlers.Stub(f => f.GetHandlerInstancesFor<IFirstInterface>())
                .Return(new[] { (IHandleMessages<IFirstInterface>)handler });

            activateHandlers.Stub(f => f.GetHandlerInstancesFor<ISecondInterface>())
                .Return(new[] { (IHandleMessages<ISecondInterface>)handler });

            activateHandlers.Stub(f => f.GetHandlerInstancesFor<PolymorphicMessage>())
                .Return(new IHandleMessages<PolymorphicMessage>[0]);

            bus.Start();

            if (!manualResetEvent.WaitOne(TimeSpan.FromSeconds(5)))
            {
                Assert.Fail("Did not receive messages withing timeout");
            }

            handler.FirstMessageHandled.ShouldBe(true);
            handler.SecondMessageHandled.ShouldBe(true);
        }

        class SomeHandler : IHandleMessages<IFirstInterface>, IHandleMessages<ISecondInterface>
        {
            readonly ManualResetEvent manualResetEvent;

            public SomeHandler(ManualResetEvent manualResetEvent)
            {
                this.manualResetEvent = manualResetEvent;
            }

            public bool FirstMessageHandled { get; set; }
            public bool SecondMessageHandled { get; set; }

            public void Handle(IFirstInterface message)
            {
                FirstMessageHandled = true;
                PossiblyRaiseEvent();
            }

            public void Handle(ISecondInterface message)
            {
                SecondMessageHandled = true;
                PossiblyRaiseEvent();
            }

            void PossiblyRaiseEvent()
            {
                if (FirstMessageHandled && SecondMessageHandled)
                {
                    manualResetEvent.Set();
                }
            }
        }

        interface IFirstInterface { }
        interface ISecondInterface { }
        class PolymorphicMessage : IFirstInterface, ISecondInterface { }
    }
}