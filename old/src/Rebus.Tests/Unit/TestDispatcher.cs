using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Rebus.Tests.Persistence.Sagas;
using Rhino.Mocks;
using Shouldly;
using Rebus.Testing;

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
                                        new DeferredMessageHandlerForTesting(),
                                        null);
        }

        [Test]
        public void ThrowsIfTwoSagaHandlersArePresentInHandlerPipeline()
        {
            // arrange
            activator.UseHandler(new FirstSaga());
            activator.UseHandler(new SecondSaga());
            var messageThatCanBeHandledByBothSagas = new SomeMessage();

            // act
            var exception =
                Should.Throw<MultipleSagaHandlersFoundException>(
                    async () => await dispatcher.Dispatch(messageThatCanBeHandledByBothSagas));

            // assert
            exception.Message.ShouldContain("FirstSaga");
            exception.Message.ShouldContain("SecondSaga");
            exception.Message.ShouldContain("SomeMessage");
        }

        [Test]
        public void DoesNotThrowIfTwoSagaHandlersArePresentInHandlerPipeline_ButSagaPersisterCanUpdateMultipleSagaDatasAtomically()
        {
            // arrange
            var fakePersister = MockRepository.GenerateMock<IStoreSagaData, ICanUpdateMultipleSagaDatasAtomically>();
            
            dispatcher = new Dispatcher(fakePersister,
                                        activator,
                                        new InMemorySubscriptionStorage(),
                                        pipelineInspector,
                                        new DeferredMessageHandlerForTesting(),
                                        null);


            activator.UseHandler(new FirstSaga());
            activator.UseHandler(new SecondSaga());
            var messageThatCanBeHandledByBothSagas = new SomeMessage();

            // act
            Assert.DoesNotThrow(() => dispatcher.Dispatch(messageThatCanBeHandledByBothSagas));
        }

        class FirstSaga : Saga<SomeSagaData>, IHandleMessages<SomeMessage>
        {
            public override void ConfigureHowToFindSaga()
            {
            }

            public void Handle(SomeMessage message)
            {
            }
        }

        class SecondSaga : Saga<SomeSagaData>, IHandleMessages<SomeMessage>
        {
            public override void ConfigureHowToFindSaga()
            {
            }

            public void Handle(SomeMessage message)
            {
            }
        }

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        [Test]
        public void ThrowsIfNoHandlersCanBeFound()
        {
            // arrange
            var theMessage = new SomeMessage();

            // act
            var ex = Should.Throw<UnhandledMessageException>(async () => await dispatcher.Dispatch(theMessage));

            // assert
            ex.UnhandledMessage.ShouldBe(theMessage);
        }

        [Test]
        public void PolymorphicDispatchWorksLikeExpected()
        {
            // arrange
            var calls = new List<string>();
            activator.UseHandler(new AnotherHandler(calls))
                .UseHandler(new YetAnotherHandler(calls))
                .UseHandler(new AuthHandler(calls));

            pipelineInspector.SetOrder(typeof (AuthHandler), typeof (AnotherHandler));

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

        interface ISomeInterface
        {
        }

        interface IAnotherInterface
        {
        }

        class SomeMessage : ISomeInterface, IAnotherInterface
        {
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

        class SmallestSagaOnEarthNotCorrelatedOnInitialMessage : Saga<SagaData>,
                                                                 IAmInitiatedBy<InitiatingMessageWithANumber>,
                                                                 IHandleMessages<SomeMessageWithANumber>
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

        class DummyHandler : IHandleMessages<object>
        {
            public void Handle(object message)
            {
                
            }
        }

        class DummySaga : Saga<SagaData>, IAmInitiatedBy<object>
        {
            public void Handle(object message)
            {

            }

            public override void ConfigureHowToFindSaga()
            {
                
            }
        }

        class BooleanHandler : IHandleMessages<object>
        {
            public bool handled = false;

            public void Handle(object message)
            {
                handled = true;
            }
        }

        class BooleanSaga : Saga<SagaData>, IAmInitiatedBy<object>
        {
            public bool Handled = false;

            public void Handle(object message)
            {
                Handled = true;
            }

            public override void ConfigureHowToFindSaga()
            {

            }
        }

        [Test]
        public void BeforeHandlingEventIsFired()
        {
            // arrange
            var fired = false;
            activator.UseHandler(new DummyHandler());
            dispatcher.BeforeHandling += (message, sagadata) =>
                {
                    fired = true;
                };

            // act
            dispatcher.Dispatch<object>(new Object()).Wait();

            // assert
            Assert.IsTrue(fired);
        }

        [Test]
        public void BeforeHandlingEventIsFiredForSagas()
        {
            // arrange
            var fired = false;
            activator.UseHandler(new DummySaga());
            dispatcher.BeforeHandling += (message, sagadata) =>
            {
                fired = true;
            };

            // act
            dispatcher.Dispatch<object>(new Object()).Wait();

            // assert
            Assert.IsTrue(fired);
        }

        [Test]
        public void AfterHandlingEventIsFired()
        {
            // arrange
            var fired = false;
            activator.UseHandler(new DummyHandler());
            dispatcher.AfterHandling += (message, sagadata) =>
            {
                fired = true;
            };

            // act
            dispatcher.Dispatch<object>(new Object()).Wait();

            // assert
            Assert.IsTrue(fired);
        }

        [Test]
        public void AfterHandlingEventIsFiredForSagas()
        {
            // arrange
            var fired = false;
            activator.UseHandler(new DummySaga());
            dispatcher.AfterHandling += (message, sagadata) =>
            {
                fired = true;
            };

            // act
            dispatcher.Dispatch<object>(new Object()).Wait();

            // assert
            Assert.IsTrue(fired);
        }

        [Test]
        public void IfBeforeHandlingNotDefinedHandlerIsExecuted()
        {
            // arrange
            var handler = new BooleanHandler();
            activator.UseHandler(handler);

            // act
            dispatcher.Dispatch<object>(new Object()).Wait();

            // assert
            Assert.IsTrue(handler.handled);
        }

        [Test]
        public void IfBeforeHandlingNotDefinedSagaHandlerIsExecuted()
        {
            // arrange
            var handler = new BooleanSaga();
            activator.UseHandler(handler);

            // act
            dispatcher.Dispatch<object>(new Object()).Wait();

            // assert
            Assert.IsTrue(handler.Handled);
        }

        [Test]
        public void OnHandlingErrorGetsCalledWhenBeforeHandlingThrowsException()
        {
            // arrange
            Exception actual = null;
            var expected = new Exception();
            var handler = new DummyHandler();
            activator.UseHandler(handler);
            dispatcher.BeforeHandling += (message, sagadata) =>
            {
                throw expected;
            };
            dispatcher.OnHandlingError += exception =>
            {
                actual = exception;
            };

            // act
            var task = dispatcher.Dispatch(new Object());

            Assert.Throws<AggregateException>(() => task.Wait());

            // assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void OnHandlingErrorGetsCalledWhenBeforeHandlingThrowsExceptionForSagas()
        {
            // arrange
            Exception actual = null;
            var expected = new Exception();
            var handler = new DummySaga();
            activator.UseHandler(handler);
            dispatcher.BeforeHandling += (message, sagadata) =>
            {
                throw expected;
            };
            dispatcher.OnHandlingError += exception =>
            {
                actual = exception;
            };

            // act
            var task = dispatcher.Dispatch(new Object());

            Assert.Throws<AggregateException>(() => task.Wait());

            // assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void OnHandlingErrorGetsCalledWhenHandlingThrowsException()
        {
            // arrange
            Exception actual = null;
            var msg = new Object();
            var expected = new Exception();
            var handler = Mock<IHandleMessages<object>>();
            handler.Expect(x => x.Handle(msg)).Throw(expected);
            activator.UseHandler(handler);
            dispatcher.OnHandlingError += exception =>
            {
                actual = exception;
            };

            // act
            var task = dispatcher.Dispatch(msg);

            Assert.Throws<AggregateException>(() => task.Wait());

            // assert
            Assert.AreEqual(expected, actual);
            handler.VerifyAllExpectations();
        }

        [Test]
        public void OnHandlingErrorGetsCalledWhenHandlingThrowsExceptionForSagas()
        {
            // arrange
            Exception actual = null;
            var msg = new Object();
            var expected = new Exception();
            var handler = Mock<IHandleMessages<object>>();
            handler.Expect(x => x.Handle(msg)).Throw(expected);
            activator.UseHandler(handler);
            dispatcher.OnHandlingError += exception =>
            {
                actual = exception;
            };

            // act
            var task = dispatcher.Dispatch(msg);

            Assert.Throws<AggregateException>(() => task.Wait());

            // assert
            Assert.AreEqual(expected, actual);
            handler.VerifyAllExpectations();
        }

        [Test]
        public void RegisteringHandlersToSkipDuringBeforeHandleSkipsHandlerInvokation()
        {
            // arrange
            TrackDisposable(TransactionContext.None());
            var handler = new BooleanHandler();
            activator.UseHandler(handler);
            var mock = Mock<IMessageContext>();
            mock.Stub(m => m.Items).Return(new Dictionary<string, object>());
            mock.Stub(m => m.Headers).Return(new Dictionary<string, object>());

            using (var fake = FakeMessageContext.Establish(mock))
            {
                dispatcher.BeforeHandling += (message, hndl) =>
                {
                    var ctx = MessageContext.GetCurrent();
                    mock.Stub(m => m.HandlersToSkip).Return(new List<Type>() { hndl.GetType() }.AsReadOnly());
                };

                // act
                dispatcher.Dispatch<object>(new Object()).Wait();
            }
            // assert
            Assert.IsFalse(handler.handled);
        }

        [Test]
        public void RegisteringHandlersToSkipDuringBeforeHandleSkipsHandlerInvokationForSagas()
        {
            // arrange
            TrackDisposable(TransactionContext.None());
            var handler = new BooleanSaga();
            activator.UseHandler(handler);

            var mock = Mock<IMessageContext>();
            mock.Stub(m => m.Items).Return(new Dictionary<string, object>());
            mock.Stub(m => m.Headers).Return(new Dictionary<string, object>());

            dispatcher.BeforeHandling += (message, hndl) =>
            {
                var ctx = MessageContext.GetCurrent();
                mock.Stub(m => m.HandlersToSkip).Return(new List<Type>() { hndl.GetType() }.AsReadOnly());
            };

            using (var fake = FakeMessageContext.Establish(mock))
            {

                // act
                dispatcher.Dispatch<object>(new Object()).Wait();
            }
            // assert
            Assert.IsFalse(handler.Handled);
        }
    }
}