using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

namespace Rebus.Msmq.Tests.Integration
{
    [TestFixture]
    [Description("Tests a scenario where a handler that awaits stuff is unfortunate and ends up being delayed because of the queue receive polling timeout")]
    public class TestUnluckyContinuations : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());

            var queueName = TestConfig.GetName("unlucky_continuations");

            MsmqUtil.PurgeQueue(queueName);

            Configure.With(_activator)
                .Transport(t => t.UseMsmq(queueName))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(5);
                })
                .Start();
        }

        [Test]
        public void MessageHandlingTimeDoesNotSufferEvenThoughTransportBlocksOnAwaitForALongTime()
        {
            var gotMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async s =>
            {
                Console.WriteLine("waiting 50 ms");
                await Task.Delay(50);

                Console.WriteLine("waiting 50 ms");
                await Task.Delay(50);

                Console.WriteLine("waiting 50 ms");
                await Task.Delay(50);

                gotMessage.Set();
            });

            _activator.Bus.SendLocal("hej!").Wait();

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void HandlersWithAwaitAreExecutedInParallel()
        {
            var resetEvents = new List<ManualResetEvent>
            {
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
                new ManualResetEvent(false),
            };

            var resetEventsQueue = new ConcurrentQueue<ManualResetEvent>(resetEvents);
            var idCounter = 0;

            _activator.Handle<string>(async message =>
            {
                var id = Interlocked.Increment(ref idCounter);

                Printt($"operation {id} (msg: {message}) sleeping 1s...");

                await MeasuredDelay(1000);

                Printt($"operation {id} done sleeping - setting reset event");

                var resetEvent = resetEventsQueue.GetNextOrThrow();

                Printt($"operation {id} set the reset event");

                resetEvent.Set();
            });

            Task.WaitAll(
                _activator.Bus.SendLocal("1"),
                _activator.Bus.SendLocal("2"),
                _activator.Bus.SendLocal("3"),
                _activator.Bus.SendLocal("4"),
                _activator.Bus.SendLocal("5"));

            var doneThing = "";
            var allDone = new ManualResetEvent(false);

            Task.WhenAll(resetEvents.Select(r => r.WaitAsync()))
                .ContinueWith(t =>
                {
                    Printt("HANDLE TASKS DONE!");
                    doneThing = "HandleTasks";
                    allDone.Set();
                });

            Task.Delay(4500)
                .ContinueWith(t =>
                {
                    Printt("TIME IS OUT!");
                    doneThing = "TIMEOUT!";
                    allDone.Set();
                });

            allDone.WaitOrDie(TimeSpan.FromSeconds(3));

            Assert.That(doneThing, Is.EqualTo("HandleTasks"));
        }
    }
}