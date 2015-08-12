using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture(Ignore = TestCategories.IgnoreLongRunningTests), Category(TestCategories.Rabbit)]
    public class DoesNotCrashWhenRunningWithSingleRabbitThread : RabbitMqFixtureBase
    {
        const string InputQueueName1 = "test.workerCount1";
        const string InputQueueName2 = "test.workerCount2";
        const string ErrorQueueName = "test.workerCount.error";

        protected override void DoSetUp()
        {
            DeleteQueue(InputQueueName1);
            DeleteQueue(InputQueueName2);
            DeleteQueue(ErrorQueueName);

            RebusLoggerFactory.Current = new ConsoleLoggerFactory(true){ShowTimestamps = true};
        }

        [TestCase(1, 10)]
        [TestCase(5, 10)]
        [TestCase(10, 10)]
        public void SeeIfItWorks(int workers, int iterations)
        {
            var receivedStrings = new List<string>();

            using (var adapter1 = new BuiltinContainerAdapter())
            using (var adapter2 = new BuiltinContainerAdapter())
            {
                adapter1.Handle<ThisIsJustSomeRandomTestMessage>(msg => receivedStrings.Add(msg.WithSomethingInside));

                var bus1 =
                    Configure.With(adapter1)
                             .Transport(x => x.UseRabbitMq(ConnectionString, InputQueueName1, ErrorQueueName)
                                              .ManageSubscriptions())
                             .CreateBus()
                             .Start(workers);

                bus1.Subscribe<ThisIsJustSomeRandomTestMessage>();

                var bus2 =
                    Configure.With(adapter2)
                             .Transport(x => x.UseRabbitMq(ConnectionString, InputQueueName2, ErrorQueueName)
                                              .ManageSubscriptions())
                             .CreateBus()
                             .Start(1);

                var messageCounter = 1;
                iterations.Times(() =>
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(2));

                        bus2.Publish(new ThisIsJustSomeRandomTestMessage{WithSomethingInside=string.Format("Message number {0}", messageCounter++)});
                    });

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }

            receivedStrings.ShouldBe(Enumerable.Range(1, iterations)
                                               .Select(i => string.Format("Message number {0}", i))
                                               .ToList());
        }

        public class ThisIsJustSomeRandomTestMessage
        {
            public string WithSomethingInside { get; set; }
        }
    }
}