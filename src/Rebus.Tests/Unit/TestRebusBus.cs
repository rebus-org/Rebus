using System;
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
    public class TestRebusBus : FixtureBase
    {
        RebusBus bus;
        MessageReceiverForTesting receiveMessages;
        HandlerActivatorForTesting activateHandlers;
        IDetermineDestination determineDestination;
        ISendMessages sendMessages;
        JsonMessageSerializer serializeMessages;
        IStoreSagaData storeSagaData;
        IInspectHandlerPipeline inspectHandlerPipeline;

        protected override void DoSetUp()
        {
            activateHandlers = new HandlerActivatorForTesting();
            determineDestination = Mock<IDetermineDestination>();
            sendMessages = Mock<ISendMessages>();
            serializeMessages = new JsonMessageSerializer();
            storeSagaData = Mock<IStoreSagaData>();
            receiveMessages = new MessageReceiverForTesting(serializeMessages);
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

            // act
            bus.Send(theMessageThatWasSent);

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("woolala"), Arg<TransportMessageToSend>.Is.Anything));
        }

        [Test]
        public void SubscribesToMessagesFromTheRightPublisher()
        {
            // arrange
            determineDestination.Stub(d => d.GetEndpointFor(typeof(PolymorphicMessage))).Return("woolala");
            receiveMessages.SetInputQueue("my input queue");

            // act
            bus.Subscribe<PolymorphicMessage>();

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("woolala"),
                                                     Arg<TransportMessageToSend>.Matches(
                                                         m => m.Headers.ContainsKey(Headers.ReturnAddress)
                                                              && m.Headers[Headers.ReturnAddress] == "my input queue")));
        }

        [Test]
        public void CanDoPolymorphicMessageDispatch()
        {
            receiveMessages.Deliver(new Message
                                        {
                                            Messages = new object[]
                                                           {
                                                               new PolymorphicMessage()
                                                           }
                                        });

            var manualResetEvent = new ManualResetEvent(false);

            var handler = new SomeHandler(manualResetEvent);

            activateHandlers.UseHandler(handler);

            bus.Start();

            if (!manualResetEvent.WaitOne(TimeSpan.FromSeconds(4)))
            {
                Assert.Fail("Did not receive messages within timeout");
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