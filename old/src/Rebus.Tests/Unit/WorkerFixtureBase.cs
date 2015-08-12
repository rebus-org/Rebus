using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Tests.Integration;

namespace Rebus.Tests.Unit
{
    internal abstract class WorkerFixtureBase : FixtureBase
    {
        protected Worker CreateWorker(IReceiveMessages receiveMessages, IActivateHandlers activateHandlers, 
            IInspectHandlerPipeline inspectHandlerPipeline = null,
            IEnumerable<IUnitOfWorkManager> unitOfWorkManagers = null,
            IErrorTracker errorTracker = null)
        {
            return new Worker(
                errorTracker ?? new ErrorTracker("error"),
                receiveMessages,
                activateHandlers,
                new InMemorySubscriptionStorage(),
                new JsonMessageSerializer(),
                new SagaDataPersisterForTesting(),
                inspectHandlerPipeline ?? new TrivialPipelineInspector(),
                "Just some test worker",
                new DeferredMessageHandlerForTesting(),
                new IncomingMessageMutatorPipelineForTesting(),
                null,
                unitOfWorkManagers ?? new IUnitOfWorkManager[0],
                new ConfigureAdditionalBehavior(),
                new MessageLogger(),
                new RebusSynchronizationContext());
        }
    }
}