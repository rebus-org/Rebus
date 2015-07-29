using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Messages;
using Rebus.Serialization.Json;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    internal class TestWorker_PolymorphicDispatch : WorkerFixtureBase
    {
        Worker worker;
        MessageReceiverForTesting receiveMessages;
        HandlerActivatorForTesting activateHandlers;
        RearrangeHandlersPipelineInspector inspectHandlerPipeline;

        protected override void DoSetUp()
        {
            receiveMessages = new MessageReceiverForTesting(new JsonMessageSerializer());
            activateHandlers = new HandlerActivatorForTesting();
            inspectHandlerPipeline = new RearrangeHandlersPipelineInspector();

            worker = CreateWorker(receiveMessages, activateHandlers, inspectHandlerPipeline);
        }

        protected override void DoTearDown()
        {
            worker.Dispose();
        }

        [Test]
        public void PolymorphicDispatchAndPipelineInspectorWorkTogetherLikeExpected()
        {
            // arrange
            var calls = new List<string>();
            activateHandlers
                .UseHandler(new ObjHandler(calls))
                .UseHandler(new FirstRoleHandler(calls))
                .UseHandler(new SecondRoleHandler(calls))
                .UseHandler(new PolyHandler(calls));

            inspectHandlerPipeline
                .SetOrder(typeof (ObjHandler),
                          typeof (PolyHandler),
                          typeof (SecondRoleHandler),
                          typeof (FirstRoleHandler));

            var message = new Message
                              {
                                  Messages = new object[]
                                                 {
                                                     new SimplePolymorphicMessage()
                                                 }
                              };

            // act
            worker.Start();

            receiveMessages.Deliver(message);

            Thread.Sleep(3000);

            // assert
            Console.WriteLine(@"Calls:
{0}",
                              string.Join(Environment.NewLine,
                                          calls.Select((c, i) => string.Format("    {0}: {1}", i, c))));

            calls.Count.ShouldBe(4);

            calls[0].ShouldBe("object");
            calls[1].ShouldBe("poly");
            calls[2].ShouldBe("second_role");
            calls[3].ShouldBe("first_role");
        }

        class TestHandlerBase<T> : IHandleMessages<T>
        {
            readonly List<string> calls;
            readonly string label;

            protected TestHandlerBase(List<string> calls, string label)
            {
                this.calls = calls;
                this.label = label;
            }

            public void Handle(T message)
            {
                calls.Add(label);
            }
        }

        class ObjHandler : TestHandlerBase<object>{
            public ObjHandler(List<string> calls) : base(calls, "object"){}
        }
        class FirstRoleHandler : TestHandlerBase<IFirstRole>{
            public FirstRoleHandler(List<string> calls) : base(calls, "first_role") { }
        }
        class SecondRoleHandler : TestHandlerBase<ISecondRole>{
            public SecondRoleHandler(List<string> calls) : base(calls, "second_role"){}
        }
        class PolyHandler : TestHandlerBase<SimplePolymorphicMessage>{
            public PolyHandler(List<string> calls) : base(calls, "poly"){}
        }

        class SimplePolymorphicMessage : IFirstRole, ISecondRole
        {
            
        }

        interface IFirstRole {}
        interface ISecondRole {}
    }
}