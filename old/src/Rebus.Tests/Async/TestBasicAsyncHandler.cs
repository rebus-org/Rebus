using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Async
{
    [TestFixture]
    public class TestBasicAsyncHandler : FixtureBase
    {
        BuiltinContainerAdapter adapter;
        const string InputQueue = "test.async.input";
        const string ErrorQueue = "test.async.error";

        protected override void DoSetUp()
        {
            adapter = TrackDisposable(new BuiltinContainerAdapter());
        }

        protected override void DoTearDown()
        {
            adapter.Dispose();
        }

        [Test]
        public void RequestReply()
        {
            var replyHandled = new ManualResetEvent(false);

            var bus = StartBus(1);

            adapter.HandleAsync<SomeMessage>(async message =>
            {
                var delay = message.Delay;
                await Task.Delay(delay);
                bus.Reply("yo!");
            });

            adapter.Handle<string>(str =>
            {
                if (str == "yo!")
                {
                    Console.WriteLine("Got reply!");
                    replyHandled.Set();
                }
            });

            bus.SendLocal(new SomeMessage { Delay = 1.Seconds() });

            Console.WriteLine("Waiting for reply...");
            replyHandled.WaitUntilSetOrDie(2.Seconds());

            Console.WriteLine("Bam!");
        }


        [Test]
        public void ContinuesOnWorkerThread()
        {
            var done = new ManualResetEvent(false);

            adapter.HandleAsync<SomeMessage>(async message =>
            {
                Console.WriteLine("Intro");
                var thread = Thread.CurrentThread.ManagedThreadId;

                await Task.Delay(message.Delay);
                Console.WriteLine("First continuation");
                Thread.CurrentThread.ManagedThreadId.ShouldBe(thread);

                await Task.Delay(message.Delay);
                Console.WriteLine("Second continuation");
                Thread.CurrentThread.ManagedThreadId.ShouldBe(thread);

                await Task.Delay(message.Delay);
                Console.WriteLine("Third and final continuation");
                Thread.CurrentThread.ManagedThreadId.ShouldBe(thread);

                done.Set();
            });

            var bus = StartBus(1);

            bus.SendLocal(new SomeMessage { Delay = TimeSpan.FromSeconds(1) });

            done.WaitUntilSetOrDie(5.Seconds());
        }

        [Test]
        public void RestoresContext()
        {
            var done = new ManualResetEvent(false);
            var result = "";

            adapter.HandleAsync<SomeMessage>(async message =>
            {
                MessageContext.GetCurrent().Items["somecontext"] = "asger";

                await Task.Delay(message.Delay);
                MessageContext.GetCurrent().Items["somecontext"] += " heller";

                await Task.Delay(message.Delay);
                MessageContext.GetCurrent().Items["somecontext"] += " hallas";

                await Task.Delay(message.Delay);
                result = MessageContext.GetCurrent().Items["somecontext"] + " waits no more!";

                done.Set();
            });

            var bus = StartBus(1);

            bus.SendLocal(new SomeMessage { Delay = TimeSpan.FromSeconds(1) });

            done.WaitUntilSetOrDie(5.Seconds());

            result.ShouldBe("asger heller hallas waits no more!");
        }

        [Test]
        public void ContinuesOnAnyWorkerThreadWithContext()
        {
            var done = new ManualResetEvent(false);
            var result = "";
            Thread initial = null;
            Thread final = null;

            adapter.HandleAsync<SomeMessage>(async message =>
            {
                initial = Thread.CurrentThread;
                MessageContext.GetCurrent().Items["somecontext"] = "inital";

                Console.WriteLine("Started on thread " + initial.ManagedThreadId);

                do
                {
                    await Task.Delay(message.Delay);
                } while (Thread.CurrentThread.ManagedThreadId == initial.ManagedThreadId);

                final = Thread.CurrentThread;
                result = MessageContext.GetCurrent().Items["somecontext"] + "final";

                Console.WriteLine("Ended on thread " + final.ManagedThreadId);

                done.Set();
            });

            var bus = StartBus(10);

            bus.SendLocal(new SomeMessage { Delay = TimeSpan.FromSeconds(1) });

            // wait for a long time, just to be sure some other worker thread will pick it up
            done.WaitUntilSetOrDie(TimeSpan.FromMinutes(1));

            result.ShouldBe("initalfinal");
            initial.ShouldNotBe(final);
            initial.Name.ShouldContain("Rebus 1 worker");
            final.Name.ShouldContain("Rebus 1 worker");
        }

        [Test]
        public void HandlesExceptionsAsUsual()
        {
            var log = new List<string>();
            var tries = 0;

            adapter.HandleAsync<SomeMessage>(async message =>
            {
                tries++;
                Console.WriteLine("Handling");
                await Task.Yield();
                throw new Exception("failed");
            });

            var bus = StartBus(1, log, numberOfRetries: 5);

            bus.SendLocal(new SomeMessage());

            Thread.Sleep(1000);

            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, log));
            Console.WriteLine("---------------------------------------------------------------------------");

            log.ShouldContain(x => x.StartsWith("Rebus.Bus.RebusBus|WARN: User exception in Rebus"));
            tries.ShouldBe(5);
        }

        [Test]
        public void MessageIsOnlyHandledOnce()
        {
            var done = new ManualResetEvent(false);

            var bus = StartBus(1);

            var i = 0;
            adapter.HandleAsync<SomeMessage>(async message =>
            {
                i++;
                await Task.Delay(5.Seconds());
                done.Set();
            });

            bus.SendLocal(new SomeMessage());

            done.WaitUntilSetOrDie(50.Seconds());

            i.ShouldBe(1);
        }

        [Test]
        public void SyncTaskRunWorks()
        {
            var done = new ManualResetEvent(false);

            adapter.Handle<SomeMessage>(message =>
            {
                Console.WriteLine("Handling");
                Task.Run(() => { }).Wait();
                done.Set();
            });

            var bus = StartBus(1);

            bus.SendLocal(new SomeMessage());

            done.WaitUntilSetOrDie(1.Seconds());
        }

        IBus StartBus(int numberOfWorkers, List<string> log = null, int? numberOfRetries = null)
        {
            MsmqUtil.PurgeQueue(InputQueue);
            MsmqUtil.PurgeQueue(ErrorQueue);

            return Configure.With(adapter)
                .Logging(l =>
                {
                    if (log == null)
                    {
                        l.ColoredConsole(minLevel: LogLevel.Warn);
                    }
                    else
                    {
                        l.Use(new ListLoggerFactory(log));
                    }
                })
                .Behavior(b =>
                {
                    if (numberOfRetries != null)
                    {
                        b.SetMaxRetriesFor<Exception>(numberOfRetries.Value);
                    }
                    else
                    {
                        b.SetMaxRetriesFor<Exception>(0);
                    }
                })
                .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                .CreateBus()
                .Start(numberOfWorkers);
        }

        public class SomeMessage
        {
            public TimeSpan Delay { get; set; }
        }

        public class SomeOtherMessage
        {
            public TimeSpan Delay { get; set; }
        }
    }
}