using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Tests.Transports.Rabbit;
using Rebus.RabbitMQ;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Category(TestCategories.Rabbit)]
    public class RabbitMqDoesNotHaveRaceConditionWhenSubscribingOnAutoDeleteQueue : RabbitMqFixtureBase
    {
        readonly List<IDisposable> stuffToDispose = new List<IDisposable>();
        IStartableBus startableBus;
        BuiltinContainerAdapter adapter;

        protected override void DoSetUp()
        {
            adapter = new BuiltinContainerAdapter();
            stuffToDispose.Add(adapter);

            startableBus = Configure.With(adapter)
                                    .Transport(t => t.UseRabbitMq(ConnectionString, "test-autodelete.input", "error")
                                                     .ManageSubscriptions()
                                                     .AutoDeleteInputQueue())
                                    .CreateBus();
        }

        protected override void DoTearDown()
        {
            stuffToDispose.ForEach(d => d.Dispose());
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void StatementOfSomethingThatMustHold(int numberOfWorkers)
        {
            var resetEvent = new ManualResetEvent(false);
            adapter.Handle<string>(str =>
                {
                    if (str == "w00t!") resetEvent.Set();
                });
            

            var bus = startableBus.Start(numberOfWorkers);
            Thread.Sleep(0.5.Seconds());
            bus.Subscribe<string>();


            bus.Publish("w00t!");
            var timeout = 3.Seconds();
            Assert.That(resetEvent.WaitOne(timeout), Is.True, "Did not receive expected event within {0} timeout", timeout);
        }
    }
}