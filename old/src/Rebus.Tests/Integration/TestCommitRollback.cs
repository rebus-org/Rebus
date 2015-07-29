using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Tests.Integration.Factories;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture(typeof(RabbitBusFactory), Category = TestCategories.Rabbit)]
    [TestFixture(typeof(MsmqBusFactory))]
    [Category(TestCategories.Integration)]
    public class TestCommitRollback<TFactory> : FixtureBase where TFactory : IBusFactory, new()
    {
        TFactory factory;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false) {MinLevel = LogLevel.Warn};
            factory = new TFactory();
        }

        protected override void DoTearDown()
        {
            factory.Cleanup();
        }

        [Test]
        public void MessageHandlingIsTransactional()
        {
            // arrange
            const string senderQueue = "test.commitrollback.sender";
            const string middlemanQueue = "test.commitrollback.middleman";
            const string recipient1Queue = "test.commitrollback.recipient1";
            const string recipient2Queue = "test.commitrollback.recipient2";

            // sender
            var sender = factory.CreateBus(senderQueue, new HandlerActivatorForTesting());

            // middleman
            var failCounter = 0;
            var middlemanHandlerActivator = new HandlerActivatorForTesting();
            var middleman = factory.CreateBus(middlemanQueue, middlemanHandlerActivator);
            middlemanHandlerActivator.Handle<string>(str =>
                {
                    failCounter++;

                    middleman.Advanced.Routing.Send(recipient1Queue, string.Format("mr. 1, this is my fail count: {0}", failCounter));
                    middleman.Advanced.Routing.Send(recipient2Queue, string.Format("mr. 2, this is my fail count: {0}", failCounter));

                    if (failCounter < 3) throw new ApplicationException("oh noes!!!!");
                });

            // two recipients
            var recipient1Received = new List<string>();
            var recipient2Received = new List<string>();
            factory.CreateBus(recipient1Queue, new HandlerActivatorForTesting().Handle<string>(recipient1Received.Add));
            factory.CreateBus(recipient2Queue, new HandlerActivatorForTesting().Handle<string>(recipient2Received.Add));

            factory.StartAll();

            Thread.Sleep(0.5.Seconds());

            // act
            sender.Advanced.Routing.Send(middlemanQueue, "hello there my man!");

            Thread.Sleep(2.Seconds());

            // assert
            failCounter.ShouldBe(3);
            recipient1Received.ShouldBe(new List<string> { "mr. 1, this is my fail count: 3" });
            recipient2Received.ShouldBe(new List<string> { "mr. 2, this is my fail count: 3" });
        }
    }
}