using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Rhino.Mocks;

namespace Rebus.Tests.Bugs
{
    public class DeferStealsOurTimeoutReplies : FixtureBase
    {
        [Test]
        public void AndItShouldNotDoThat()
        {
            var handlerActivator = new HandlerActivatorForTesting();
            var pipelineInspector = new TrivialPipelineInspector();
            var handleDeferredMessage = Mock<IHandleDeferredMessage>();
            var dispatcher = new Dispatcher(new InMemorySagaPersister(),
                                        handlerActivator,
                                        new InMemorySubscriptionStorage(),
                                        pipelineInspector,
                                        handleDeferredMessage);

            dispatcher.Dispatch(new TimeoutReply
            {
                CorrelationId = TimeoutReplyHandler.TimeoutReplySecretCorrelationId,
                CustomData = TimeoutReplyHandler.Serialize(new Message { Id = "1" })
            });

            dispatcher.Dispatch(new TimeoutReply
            {
                CustomData = TimeoutReplyHandler.Serialize(new Message { Id = "2" })
            });

            handleDeferredMessage.AssertWasCalled(x => x.Dispatch(Arg<Message>.Is.Anything), x => x.Repeat.Once());
            handleDeferredMessage.AssertWasCalled(x => x.Dispatch(Arg<Message>.Matches(y => y.Id == "1")));
        }

        public class Message
        {
            public string Id { get; set; }
        }
    }
}