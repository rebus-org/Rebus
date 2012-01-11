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
                                        pipelineInspector);
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

        interface ISomeInterface { }
        interface IAnotherInterface { }
        class SomeMessage : ISomeInterface, IAnotherInterface { }

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