using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Serialization.Json;

namespace Rebus.Tests.Unit
{
    public abstract class RebusBusUnitTestBase : FixtureBase
    {
        protected RebusBus bus;
        protected MessageReceiverForTesting receiveMessages;
        protected HandlerActivatorForTesting activateHandlers;
        protected IDetermineMessageOwnership determineMessageOwnership;
        protected ISendMessages sendMessages;
        protected JsonMessageSerializer serializeMessages;
        protected IStoreSagaData storeSagaData;
        protected IInspectHandlerPipeline inspectHandlerPipeline;
        protected IStoreSubscriptions storeSubscriptions;

        protected override void DoSetUp()
        {
            activateHandlers = new HandlerActivatorForTesting();
            determineMessageOwnership = Mock<IDetermineMessageOwnership>();
            sendMessages = Mock<ISendMessages>();
            serializeMessages = new JsonMessageSerializer();
            storeSagaData = Mock<IStoreSagaData>();
            receiveMessages = new MessageReceiverForTesting(serializeMessages);
            inspectHandlerPipeline = new TrivialPipelineInspector();
            storeSubscriptions = Mock<IStoreSubscriptions>();
            bus = CreateTheBus();
            bus.Start();
        }

        protected RebusBus CreateTheBus()
        {
            return new RebusBus(activateHandlers,
                                sendMessages,
                                receiveMessages,
                                storeSubscriptions,
                                storeSagaData,
                                determineMessageOwnership, serializeMessages, inspectHandlerPipeline,
                                new ErrorTracker("error"),
                                null,
                                new ConfigureAdditionalBehavior());
        }

        protected override void DoTearDown()
        {
            bus.Dispose();
        }
    }
}