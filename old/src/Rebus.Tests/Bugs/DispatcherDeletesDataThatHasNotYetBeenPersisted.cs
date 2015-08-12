using System;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Rhino.Mocks;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class DispatcherDeletesDataThatHasNotYetBeenPersisted : FixtureBase
    {
        Dispatcher dispatcher;
        HandlerActivatorForTesting activator;
        RearrangeHandlersPipelineInspector pipelineInspector;
        IStoreSagaData sagaPersister;

        protected override void DoSetUp()
        {
            activator = new HandlerActivatorForTesting();
            pipelineInspector = new RearrangeHandlersPipelineInspector();
            sagaPersister = Mock<IStoreSagaData>();
            dispatcher = new Dispatcher(sagaPersister,
                                        activator,
                                        new InMemorySubscriptionStorage(),
                                        pipelineInspector,
                                        new DeferredMessageHandlerForTesting(),
                                        null);
        }

        [Test]
        public void WillNotDeleteSagaDataThatHasNotYetBeenPersisted()
        {
            activator.UseHandler(new SomeSaga());
            dispatcher.Dispatch(new SomeMessage());
            sagaPersister.AssertWasNotCalled(x => x.Delete(Arg<ISagaData>.Is.Anything));
        }

        public class SomeSaga : Saga<SomeSagaData>, IAmInitiatedBy<SomeMessage>
        {
            public override void ConfigureHowToFindSaga()
            {
            }

            public void Handle(SomeMessage message)
            {
                MarkAsComplete();
            }
        }

        public class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        public class SomeMessage
        {
        }
    }
}