using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Shouldly;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestWorker_ErrorHandling : FixtureBase
    {
        MessageReceiverForTesting receiveMessages;
        Worker worker;
        HandlerActivatorForTesting handlerActivatorForTesting;

        protected override void DoSetUp()
        {
            receiveMessages = new MessageReceiverForTesting(new JsonMessageSerializer());
            handlerActivatorForTesting = new HandlerActivatorForTesting();

            worker = new Worker(new ErrorTracker("error"),
                                receiveMessages,
                                handlerActivatorForTesting,
                                new InMemorySubscriptionStorage(),
                                new JsonMessageSerializer(),
                                new InMemorySagaPersister(),
                                new TrivialPipelineInspector(), "Just some test worker",
                                new DeferredMessageHandlerForTesting(),
                                new IncomingMessageMutatorPipelineForTesting());
        }

        [Test]
        public void CanRaiseEventWithProperExceptionInformation()
        {
            // arrange
            var exceptions = new List<Exception>();
            var manualResetEvent = new ManualResetEvent(false);
            
            worker.UserException += (w, exception) => exceptions.Add(exception);
            worker.MessageFailedMaxNumberOfTimes += (message, text) => manualResetEvent.Set();

            handlerActivatorForTesting.UseHandler(new MyMessageHandler());

            worker.Start();

            // act
            receiveMessages.Deliver(new Message {Messages = new object[] {"woot!"}});
            
            if (!manualResetEvent.WaitOne(500.Seconds())) Assert.Fail("Message was not delivered within timeout!");

            // assert
            exceptions.Count.ShouldBe(5);
            var firstException = exceptions[0];
            var exceptionToString = firstException.ToString();

            exceptionToString.ShouldContain("InvalidOperationException");
            exceptionToString.ShouldContain("This is the error message I want to see!");
            exceptionToString.ShouldContain(typeof(MyMessageHandler).Name);
            exceptionToString.ShouldContain(typeof(MyMessageHandler).Namespace);
            exceptionToString.ShouldNotContain("TargetInvocationException");
        }

        class MyMessageHandler : IHandleMessages<string>
        {
            public void Handle(string message)
            {
                throw new InvalidOperationException("This is the error message I want to see!");
            }
        }
    }
}