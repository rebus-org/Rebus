using System;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Rhino.Mocks;

namespace Rebus.Tests.Bugs
{
    public class SagaPersisterShouldNotIndexSamePropertyTwice : FixtureBase
    {
        Dispatcher dispatcher;
        HandlerActivatorForTesting activator;
        IStoreSagaData storeSagaData;

        protected override void DoSetUp()
        {
            activator = new HandlerActivatorForTesting();
            storeSagaData = Mock<IStoreSagaData>();
            dispatcher = new Dispatcher(storeSagaData,
                                        activator,
                                        new InMemorySubscriptionStorage(),
                                        new RearrangeHandlersPipelineInspector(),
                                        new DeferredMessageHandlerForTesting(),
                                        null);
        }

        [Test]
        public void SagaWithTwoOfTheSameCorrelationsWillWork()
        {
            var saga = new SagaCorrelatedWithSameFieldTwice();
            activator.UseHandler(saga);
            dispatcher.Dispatch(new InitiatingMessageWithANumber(1));
            storeSagaData.AssertWasCalled(x => x.Insert(Arg<ISagaData>.Is.Anything, Arg<string[]>.List.Equal(new[] { "TheNumber" })));
        }

        class SagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public int TheNumber { get; set; }
        }

        class SomeMessageWithANumber
        {
            public SomeMessageWithANumber(int theNumber)
            {
                TheNumber = theNumber;
            }

            public int TheNumber { get; private set; }
        }

        class InitiatingMessageWithANumber
        {
            public InitiatingMessageWithANumber(int theNumber)
            {
                TheNumber = theNumber;
            }

            public int TheNumber { get; private set; }
        }

        class SagaCorrelatedWithSameFieldTwice : Saga<SagaData>, IAmInitiatedBy<InitiatingMessageWithANumber>, IHandleMessages<SomeMessageWithANumber>
        {
            public void Handle(SomeMessageWithANumber message) { }
            public void Handle(InitiatingMessageWithANumber message) { }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<InitiatingMessageWithANumber>(m => m.TheNumber).CorrelatesWith(d => d.TheNumber);
                Incoming<SomeMessageWithANumber>(m => m.TheNumber).CorrelatesWith(d => d.TheNumber);
            }
        }

    }
}