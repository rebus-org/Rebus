using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestDispatcher : FixtureBase
    {
        Dispatcher dispatcher;
        HandlerActivatorForTesting activator;
        RearrangeHandlersPipelineInspector pipelineInspector;

        protected override void DoSetUp()
        {
            activator = new HandlerActivatorForTesting();
            pipelineInspector = new RearrangeHandlersPipelineInspector();
            dispatcher = new Dispatcher(new InMemorySagaPersister(),
                                        activator,
                                        new InMemorySubscriptionStorage(),
                                        pipelineInspector,
                                        new DeferredMessageHandlerForTesting());
        }

        [Test]
        public void PolymorphicDispatchWorksLikeExpected()
        {
            // arrange
            var calls = new List<string>();
            activator.UseHandler(new AnotherHandler(calls))
                .UseHandler(new YetAnotherHandler(calls))
                .UseHandler(new AuthHandler(calls));

            pipelineInspector.SetOrder(typeof(AuthHandler), typeof(AnotherHandler));

            // act
            dispatcher.Dispatch(new SomeMessage());

            // assert
            calls.Count.ShouldBe(5);
            calls[0].ShouldBe("AuthHandler: object");
            calls[1].ShouldStartWith("AnotherHandler");
            calls[2].ShouldStartWith("AnotherHandler");
            calls[3].ShouldStartWith("AnotherHandler");
            calls[4].ShouldBe("YetAnotherHandler: another_interface");
        }

        [Test]
        public void NewSagaIsMarkedAsSuch()
        {
            var saga = new SmallestSagaOnEarthCorrelatedOnInitialMessage();
            activator.UseHandler(saga);
            dispatcher.Dispatch(new SomeMessageWithANumber(1));
            saga.IsNew.ShouldBe(true);
        }

        [Test]
        public void SagaInitiatedTwiceIsNotMarkedAsNewTheSecondTime()
        {
            var saga = new SmallestSagaOnEarthCorrelatedOnInitialMessage();
            activator.UseHandler(saga);
            dispatcher.Dispatch(new SomeMessageWithANumber(1));
            dispatcher.Dispatch(new SomeMessageWithANumber(1));
            saga.IsNew.ShouldBe(false);
        }

        [Test]
        public void OneMessageCanCorrelateWithSeveralSagas()
        {
            var saga = new SmallestSagaOnEarthNotCorrelatedOnInitialMessage();
            activator.UseHandler(saga);

            // initiate two sagas with the same number
            dispatcher.Dispatch(new InitiatingMessageWithANumber(1));
            dispatcher.Dispatch(new InitiatingMessageWithANumber(1));

            dispatcher.Dispatch(new SomeMessageWithANumber(1));
            saga.TimesHandlingSomeMessageWithANumber.ShouldBe(2);
        }

        interface ISomeInterface { }
        interface IAnotherInterface { }
        class SomeMessage : ISomeInterface, IAnotherInterface { }
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


        class SmallestSagaOnEarthCorrelatedOnInitialMessage : Saga<SagaData>, IAmInitiatedBy<SomeMessageWithANumber>
        {
            public void Handle(SomeMessageWithANumber message)
            {
                Data.TheNumber = message.TheNumber;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<SomeMessageWithANumber>(m => m.TheNumber).CorrelatesWith(d => d.TheNumber);
            }
        }

        class SmallestSagaOnEarthNotCorrelatedOnInitialMessage : Saga<SagaData>, IAmInitiatedBy<InitiatingMessageWithANumber>, IHandleMessages<SomeMessageWithANumber>
        {
            public int TimesHandlingSomeMessageWithANumber { get; set; }

            public void Handle(SomeMessageWithANumber message)
            {
                TimesHandlingSomeMessageWithANumber++;
            }

            public void Handle(InitiatingMessageWithANumber message)
            {
                Data.TheNumber = message.TheNumber;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<SomeMessageWithANumber>(m => m.TheNumber).CorrelatesWith(d => d.TheNumber);
            }
        }

        class SagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public int TheNumber { get; set; }
        }

        class AuthHandler : IHandleMessages<object>
        {
            readonly List<string> calls;

            public AuthHandler(List<string> calls)
            {
                this.calls = calls;
            }

            public void Handle(object message)
            {
                calls.Add("AuthHandler: object");
            }
        }

        class AnotherHandler : IHandleMessages<ISomeInterface>, IHandleMessages<object>,
            IHandleMessages<IAnotherInterface>
        {
            readonly List<string> calls;

            public AnotherHandler(List<string> calls)
            {
                this.calls = calls;
            }

            public void Handle(ISomeInterface message)
            {
                calls.Add("AnotherHandler: some_interface");
            }

            public void Handle(object message)
            {
                calls.Add("AnotherHandler: object");
            }

            public void Handle(IAnotherInterface message)
            {
                calls.Add("AnotherHandler: another_interface");
            }
        }

        class YetAnotherHandler : IHandleMessages<IAnotherInterface>
        {
            readonly List<string> calls;

            public YetAnotherHandler(List<string> calls)
            {
                this.calls = calls;
            }

            public void Handle(IAnotherInterface message)
            {
                calls.Add("YetAnotherHandler: another_interface");
            }
        }
    }

}