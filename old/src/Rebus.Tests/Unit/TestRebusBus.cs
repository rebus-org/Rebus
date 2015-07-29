using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Raven.Imports.Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Extensions;
using Rebus.Extensions.AssemblyScanning;
using Rebus.Messages;
using Rebus.Shared;
using Rebus.Testing;
using Rebus.Transports;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestRebusBus : RebusBusUnitTestBase
    {
        [Test]
        public void ThrowsWhenUsingBusThatHasNotBeenStarted()
        {
            var unstartedBus = CreateTheBus();
            var invalidOperationException = Should.Throw<InvalidOperationException>(() => unstartedBus.Routing.Send("somewhere", "yo dawg!!"));

            invalidOperationException.Message.ShouldContain("not been started");
        }

        [Test]
        public void ThrowsWhenStartingBusTwice()
        {
            // arrange
            var unstartedBus = CreateTheBus();
            unstartedBus.Start();

            // act
            var invalidOperationException = Should.Throw<InvalidOperationException>(() => unstartedBus.Start());

            // assert
            invalidOperationException.Message.ShouldContain("cannot start bus twice");

        }

        [Test, Description(@"Tests that multiple message types will be properly batched if they are owned by the same endpoint. This
is just because there was a bug some time when the grouping of the messages was wrong.")]
        public void WillHappilyIncludeMessagesOfDifferentTypeInSameTransportMessageAlsoWhenPublishing()
        {
            // arrange
            var someSubscriberEndpoint = "some-subscriber";
            storeSubscriptions.Stub(s => s.GetSubscribers(typeof(FirstMessage))).Return(new[] { someSubscriberEndpoint });
            storeSubscriptions.Stub(s => s.GetSubscribers(typeof(SecondMessage))).Return(new[] { someSubscriberEndpoint });

            // act
            var firstMessage = new FirstMessage();
            var secondMessage = new SecondMessage();

            bus.Batch.Publish(new object[] { firstMessage, secondMessage });

            // assert

            // check that the endpoint is right
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal(someSubscriberEndpoint),
                                                     Arg<TransportMessageToSend>.Is.Anything,
                                                     Arg<ITransactionContext>.Is.Anything),
                                         o => o.Repeat.Once());
        }


        [Test, Description(@"Tests that multiple message types will be properly batched if they are owned by the same endpoint. This
is just because there was a bug some time when the grouping of the messages was wrong.")]
        public void WillHappilyIncludeMessagesOfDifferentTypeInSameTransportMessage()
        {
            // arrange
            var firstMessage = new FirstMessage();
            var secondMessage = new SecondMessage();
            var thirdMessage = new SomeRandomMessage();

            var someEndpoint = "some-endpoint";
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(FirstMessage))).Return(someEndpoint);
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(SecondMessage))).Return(someEndpoint);
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(SomeRandomMessage))).Return(someEndpoint);

            // act
            bus.Batch.Send(new object[] { firstMessage, secondMessage, thirdMessage });

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal(someEndpoint),
                Arg<TransportMessageToSend>.Is.Anything,
                Arg<ITransactionContext>.Is.Anything),
                                                            o => o.Repeat.Once());
        }

        [Test]
        public void CanDoBatchReply()
        {
            // arrange
            const string returnAddress = "some random return address";
            var firstMessage = new FirstMessage();
            var secondMessage = new SecondMessage();
            var someRandomMessage = new SomeRandomMessage();
            var fakeContext = Mock<IMessageContext>();
            fakeContext.Stub(s => s.ReturnAddress).Return(returnAddress);
            fakeContext.Stub(s => s.Headers).Return(new Dictionary<string, object>());
            fakeContext.Stub(s => s.Items).Return(new Dictionary<string, object>());

            // act
            using (new NoTransaction())
            using (FakeMessageContext.Establish(fakeContext))
            {
                bus.Batch.Reply(new object[] {firstMessage, secondMessage, someRandomMessage});
            }

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal(returnAddress),
                                                     Arg<TransportMessageToSend>.Is.Anything,
                                                     Arg<ITransactionContext>.Is.Anything),
                                         o => o.Repeat.Once());
        }

        [Test]
        public void DoesNotThrowIfReturnAddressIsSpecifiedMultipleTimesInConsistentManner()
        {
            // arrange
            var someMessage = new FirstMessage();
            var anotherMessage = new SecondMessage();

            bus.AttachHeader(someMessage, Headers.ReturnAddress, "same-endpoint");
            bus.AttachHeader(anotherMessage, Headers.ReturnAddress, "same-endpoint");

            // act
            // assert
            Assert.DoesNotThrow(() => bus.Batch.Send(new object[] { someMessage, anotherMessage }));

            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Anything,
                                                     Arg<TransportMessageToSend>.Matches(t => true),
                                                     Arg<ITransactionContext>.Is.Anything));
        }

        [Test]
        public void ThrowsIfReturnAddressIsInconsistentlySpecified()
        {
            // arrange
            var someMessage = new FirstMessage();
            var anotherMessage = new SecondMessage();

            bus.AttachHeader(someMessage, Headers.ReturnAddress, "some-endpoint");
            bus.AttachHeader(anotherMessage, Headers.ReturnAddress, "another-endpoint");

            // act
            // assert
            Assert.Throws<InconsistentReturnAddressException>(() => bus.Batch.Send(new object[] { someMessage, anotherMessage }));
        }

        [Test, Description(@"Tests that headers associated with a message don't get deleted after the first time that
message is sent - because it shouldn't be prohibited to have a single message instance
and send it multiple times.

Or should it?")]
        public void WillHappilyAttachTheSameHeaderTwice()
        {
            // arrange
            var someRandomMessage = new SomeRandomMessage();

            bus.AttachHeader(someRandomMessage, "some-key", "some-value");

            // act
            bus.Routing.Send("somewhere", someRandomMessage);
            bus.Routing.Send("somewhereElse", someRandomMessage);

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("somewhere"), Arg<TransportMessageToSend>.Matches(t => t.Headers.ContainsKey("some-key")), Arg<ITransactionContext>.Is.Anything));
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("somewhereElse"), Arg<TransportMessageToSend>.Matches(t => t.Headers.ContainsKey("some-key")), Arg<ITransactionContext>.Is.Anything));
        }

        [Test]
        public void CanPublishBatchOfMessages()
        {
            // arrange
            storeSubscriptions.Stub(s => s.GetSubscribers(typeof(FirstMessage))).Return(new[] { "first-sub1", "first-sub2" });
            storeSubscriptions.Stub(s => s.GetSubscribers(typeof(SecondMessage))).Return(new[] { "second-sub1", "second-sub2" });

            // act
            var firstMessage1 = new FirstMessage();
            var firstMessage2 = new FirstMessage();
            var secondMessage1 = new SecondMessage();
            var secondMessage2 = new SecondMessage();

            bus.AttachHeader(firstMessage1, "firstMessage1", "foo");
            bus.AttachHeader(firstMessage2, "firstMessage2", "foo");
            bus.AttachHeader(secondMessage1, "secondMessage1", "foo");
            bus.AttachHeader(secondMessage2, "secondMessage2", "foo");

            bus.Batch.Publish(new object[] { firstMessage1, secondMessage1, firstMessage2, secondMessage2 });

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("first-sub1"),
                Arg<TransportMessageToSend>.Matches(t => t.Headers.ContainsKey("firstMessage1") && t.Headers.ContainsKey("firstMessage2")), Arg<ITransactionContext>.Is.Anything));

            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("first-sub2"),
                Arg<TransportMessageToSend>.Matches(t => t.Headers.ContainsKey("firstMessage1") && t.Headers.ContainsKey("firstMessage2")), Arg<ITransactionContext>.Is.Anything));

            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("second-sub1"),
                Arg<TransportMessageToSend>.Matches(t => t.Headers.ContainsKey("secondMessage1") && t.Headers.ContainsKey("secondMessage2")), Arg<ITransactionContext>.Is.Anything));

            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("second-sub2"),
                Arg<TransportMessageToSend>.Matches(t => t.Headers.ContainsKey("secondMessage1") && t.Headers.ContainsKey("secondMessage2")), Arg<ITransactionContext>.Is.Anything));
        }

        class FirstMessage { }
        class SecondMessage { }

        [Test]
        public void ThrowsIfInconsistentTimeToBeReceivedHeadersAreIncluded()
        {
            // arrange
            var anotherRandomMessage = new SomeRandomMessage();
            var someRandomMessage = new SomeRandomMessage();
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(SomeRandomMessage))).Return("whatever");

            bus.AttachHeader(someRandomMessage, Headers.TimeToBeReceived, "00:00:05");
            bus.AttachHeader(anotherRandomMessage, Headers.TimeToBeReceived, "00:00:10");

            // act
            var invalidOperationException = Assert.Throws<InconsistentTimeToBeReceivedException>(() => bus.Batch.Send(new object[] { someRandomMessage, anotherRandomMessage }));

            // assert
            //invalidOperationException.Message.ShouldContain("00:00:05");
            //invalidOperationException.Message.ShouldContain("00:00:10");
        }

        [Test]
        public void ThrowsIfInconsistentTimeToBeReceivedHeadersAreIncludedAlsoWhenFirstMessageIsSetToBeReliable()
        {
            // arrange
            var someRandomMessage = new SomeRandomMessage();
            var anotherRandomMessage = new SomeRandomMessage();
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(SomeRandomMessage))).Return("whatever");

            bus.AttachHeader(anotherRandomMessage, Headers.TimeToBeReceived, "00:00:05");

            // act
            var invalidOperationException = Assert.Throws<InconsistentTimeToBeReceivedException>(() => bus.Batch.Send(new object[] { someRandomMessage, anotherRandomMessage }));

            // assert
            //invalidOperationException.Message.ShouldContain("00:00:05");
        }

        [Test]
        public void ThrowsIfInconsistentTimeToBeReceivedHeadersAreIncludedAlsoWhenSecondMessageIsSetToBeReliable()
        {
            // arrange
            var anotherRandomMessage = new SomeRandomMessage();
            var someRandomMessage = new SomeRandomMessage();
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(SomeRandomMessage))).Return("whatever");

            bus.AttachHeader(someRandomMessage, Headers.TimeToBeReceived, "00:00:05");

            // act
            var invalidOperationException = Assert.Throws<InconsistentTimeToBeReceivedException>(() => bus.Batch.Send(new object[] { someRandomMessage, anotherRandomMessage }));

            // assert
            //invalidOperationException.Message.ShouldContain("00:00:05");
        }

        [Test]
        public void AttachesHeadersFromMessageToOutgoingMessage()
        {
            // arrange
            var someRandomMessage = new SomeRandomMessage();
            bus.AttachHeader(someRandomMessage, Headers.TimeToBeReceived, "00:00:05");

            // act
            bus.Routing.Send("hardcoded.endpoint.to.skip.lookup", someRandomMessage);

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("hardcoded.endpoint.to.skip.lookup"),
                                                     Arg<TransportMessageToSend>.Matches(t => t.Headers.ContainsKey(Headers.TimeToBeReceived)),
                                                     Arg<ITransactionContext>.Is.Anything));
        }

        class SomeRandomMessage { }

        [Test]
        public void SendsMessagesToTheRightDestination()
        {
            // arrange
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(PolymorphicMessage))).Return("woolala");
            var theMessageThatWasSent = new PolymorphicMessage();

            // act
            bus.Send(theMessageThatWasSent);

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("woolala"),
                                                     Arg<TransportMessageToSend>.Is.Anything,
                                                     Arg<ITransactionContext>.Is.Anything));
        }

        [Test]
        public void SendsMessagesToTheRightDestinationAlsoWhenSendingBatch()
        {
            // arrange
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(PolymorphicMessage))).Return("polymorphic message endpoint");
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(SomeRandomMessage))).Return("some random message endpoint");
            var firstMessage = new PolymorphicMessage();
            var secondMessage = new SomeRandomMessage();

            // act
            bus.Batch.Send(new object[] { firstMessage, secondMessage });

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("polymorphic message endpoint"),
                                                     Arg<TransportMessageToSend>.Is.Anything,
                                                     Arg<ITransactionContext>.Is.Anything));

            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("some random message endpoint"),
                                                     Arg<TransportMessageToSend>.Is.Anything,
                                                     Arg<ITransactionContext>.Is.Anything));
        }

        [Test]
        public void SubscribesToMessagesFromTheRightPublisher()
        {
            // arrange
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(PolymorphicMessage))).Return("woolala");
            receiveMessages.SetInputQueue("my input queue");

            // act
            bus.Subscribe<PolymorphicMessage>();

            // assert
            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal("woolala"),
                                                     Arg<TransportMessageToSend>.Matches(
                                                         m => m.Headers.ContainsKey(Headers.ReturnAddress)
                                                              && m.Headers[Headers.ReturnAddress].ToString() == "my input queue"),
                                                              Arg<ITransactionContext>.Is.Anything));
        }

        [Test]
        public void CanScanAssemblyAndSubscribeToMessages()
        {
            // arrange
            // Create two assemblies with handlers for int, string and byte that are declared publicly and privately.
            var parameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            parameters.ReferencedAssemblies.Add("Rebus.dll");

            const string code = @"
using Rebus;
using System.Threading.Tasks;

namespace NS { 
public class A : IHandleMessages<int>, IHandleMessages<string> { 
    public void Handle(int message) { } 
    public void Handle(string message) { }
}

public class B : IHandleMessages<int> { 
    public void Handle(int message) { }
} 

class C : IHandleMessages<byte> { 
    public void Handle(byte message) { } 
} 

class D : IHandleMessagesAsync<long> { 
    public async Task Handle(long message) { } 
} 
}";
            
            var assembly1 = CodeDomProvider.CreateProvider("CSharp").CompileAssemblyFromSource(parameters, code).CompiledAssembly;
            var assembly2 = CodeDomProvider.CreateProvider("CSharp").CompileAssemblyFromSource(parameters, code).CompiledAssembly;

            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(int))).Return("int message endpoint");
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(string))).Return("string message endpoint");
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(byte))).Return("byte message endpoint");
            determineMessageOwnership.Stub(d => d.GetEndpointFor(typeof(long))).Return("long message endpoint");
            receiveMessages.SetInputQueue("my input queue");

            // act
            bus.SubscribeByScanningForHandlers(assembly1, assembly2);

            // assert
            Action<Type, string> assertSubscriptionMessageCalledForType = (t, endPointName) =>
            {
                sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Equal(endPointName),
                                         Arg<TransportMessageToSend>.Matches(
                                             m => m.Label == "Rebus.Messages.SubscriptionMessage"
                                                  && JsonConvert.DeserializeObject<SubscriptionMessage[]>(Encoding.UTF7.GetString(m.Body))[0].Type == t.AssemblyQualifiedName),
                                                  Arg<ITransactionContext>.Is.Anything));
            };

            assertSubscriptionMessageCalledForType(typeof(int), "int message endpoint");
            assertSubscriptionMessageCalledForType(typeof(string), "string message endpoint");
            assertSubscriptionMessageCalledForType(typeof(byte), "byte message endpoint");
            assertSubscriptionMessageCalledForType(typeof(long), "long message endpoint");
        }

        [Test]
        public void CanDoPolymorphicMessageDispatch()
        {
            var manualResetEvent = new ManualResetEvent(false);
            var handler = new SomeHandler(manualResetEvent);
            activateHandlers.UseHandler(handler);

            receiveMessages.Deliver(new Message
                {
                    Messages = new object[]
                        {
                            new PolymorphicMessage()
                        }
                });

            if (!manualResetEvent.WaitOne(TimeSpan.FromSeconds(4)))
            {
                Assert.Fail("Did not receive messages within timeout");
            }

            handler.FirstMessageHandled.ShouldBe(true);
            handler.SecondMessageHandled.ShouldBe(true);
        }

        [Test]
        public void ThrowsWhenUsingDeferInOneWayMode()
        {
            IBus bus = CreateBusInOneWayMode().Start();
            TimeSpan deferTime = TimeSpan.FromMinutes(5);

            Assert.That(() => bus.Defer(deferTime, new {msg = "foo"}), Throws.InvalidOperationException);
        }

        [Test]
        public void AcceptsDeferInOneWayModeWhenReturnAddressHasBeenManuallySpecified()
        {
            var bus = CreateBusInOneWayMode().Start();
            var deferTime = TimeSpan.FromMinutes(5);
            var message = new { msg = "foo" };
            bus.AttachHeader(message, Headers.ReturnAddress, "this is not really an endpoint, but who cares :)");

            Assert.That(() => bus.Defer(deferTime, message), Throws.Nothing);
        }

        [Test]
        public void AttachesRebusMessageIdOnVirginMessage()
        {
            bus.Send(new FirstMessage());

            sendMessages.AssertWasCalled(s => s.Send(Arg<string>.Is.Anything,
                                                     Arg<TransportMessageToSend>.Matches(t => t.Headers.ContainsKey(Headers.MessageId)),
                                                     Arg<ITransactionContext>.Is.Anything));
        }

        [Test]
        public void TransfersMessageIdToDeferredMessage()
        {
            var headers = new Dictionary<string, object>
            {
                {Headers.MessageId, "Oh the uniqueness"}
            };

            var message = new FirstMessage();

            using(new NoTransaction())
            using(MessageContext.Establish(headers))
            {
                bus.Defer(TimeSpan.Zero, message);
            }

            bus.GetHeaderFor(message, Headers.MessageId).ShouldBe("Oh the uniqueness");
        }

        [Test]
        public void UserCannotOverwriteRebusMessageId()
        {
            Should.Throw<ArgumentException>(() => bus.AttachHeader(new object(), Headers.MessageId, "anything"));
        }


        RebusBus CreateBusInOneWayMode()
        {
            var behavior = new ConfigureAdditionalBehavior();
            behavior.EnterOneWayClientMode();
            return new RebusBus(activateHandlers, sendMessages, new OneWayClientGag(), storeSubscriptions, storeSagaData, determineMessageOwnership,
                serializeMessages, inspectHandlerPipeline, new ErrorTracker("error"), null, behavior);
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

        [Test]
        public void TransportMessageSentEventIsRaisedWhenMessageIsSent()
        {
            // Arrange
            var bus = CreateTheBus();
            var fired = false;
            bus.Events.BeforeInternalSend += (destination, message, published) =>
                {
                    fired = true;
                };
            bus.Start();

            // Act
            bus.Send<object>(new Object());

            // Assert
            Assert.IsTrue(fired);
        }
    }
}