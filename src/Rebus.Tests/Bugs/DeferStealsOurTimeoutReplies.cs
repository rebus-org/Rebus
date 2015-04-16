using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    public class DeferStealsOurTimeoutReplies : FixtureBase
    {
        [Test]
        public void AndItShouldNotDoThat()
        {
            var handlerActivator = new HandlerActivatorForTesting();
            var pipelineInspector = new TrivialPipelineInspector();
            var handleDeferredMessage = new MockDeferredMessageHandler();
            var dispatcher = new Dispatcher(new InMemorySagaPersister(),
                                        handlerActivator,
                                        new InMemorySubscriptionStorage(),
                                        pipelineInspector,
                                        handleDeferredMessage,
                                        null);

            dispatcher.Dispatch(new TimeoutReply
            {
                CorrelationId = TimeoutReplyHandler.TimeoutReplySecretCorrelationId,
                CustomData = TimeoutReplyHandler.Serialize(new Message { Id = "1" })
            });

            dispatcher.Dispatch(new TimeoutReply
            {
                CustomData = TimeoutReplyHandler.Serialize(new Message { Id = "2" })
            });

            handleDeferredMessage.DispatchedMessages.Count.ShouldBe(1);
            var dispatchedMessage = handleDeferredMessage.DispatchedMessages[0];
            dispatchedMessage.ShouldBeOfType<Message>();
            ((Message)dispatchedMessage).Id.ShouldBe("1");
        }

        /// <summary>
        /// Manually implemented mock because DynamicProxy cannot dynamically subclass internal types.
        /// </summary>
        class MockDeferredMessageHandler : IHandleDeferredMessage
        {
            readonly List<object> dispatchedMessages = new List<object>();

            public List<object> DispatchedMessages
            {
                get { return dispatchedMessages; }
            }

            public void DispatchLocal(object deferredMessage, Guid sagaId, IDictionary<string, object> headers)
            {
                dispatchedMessages.Add(deferredMessage);
            }

            public void SendReply(string recipient, TimeoutReply reply, Guid sagaId)
            {
                
            }
        }

        public class Message
        {
            public string Id { get; set; }
        }
    }
}