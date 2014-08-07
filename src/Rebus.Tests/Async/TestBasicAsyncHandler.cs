using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Async
{
    [TestFixture]
    public class TestBasicAsyncHandler : FixtureBase
    {
        const string InputQueue = "test.async.input";
        const string ErrorQueue = "test.async.error";

        [Test]
        public void RequestReply()
        {
            using (var adapter = TrackDisposable(new BuiltinContainerAdapter()))
            {
                var replyHandled = new ManualResetEvent(false);

                var bus = Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                    .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                    .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                    .CreateBus()
                    .Start(1);

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

                adapter.Bus.SendLocal(new SomeMessage { Delay = 1.Seconds() });

                Console.WriteLine("Waiting for reply...");
                replyHandled.WaitUntilSetOrDie(2.Seconds());

                Console.WriteLine("Bam!");
            }
        }

        [Test]
        public void ContinuesOnWorkerThread()
        {
            using (var adapter = TrackDisposable(new BuiltinContainerAdapter()))
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

                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                    .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                    .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                    .CreateBus()
                    .Start(1);

                adapter.Bus.SendLocal(new SomeMessage { Delay = TimeSpan.FromSeconds(1) });

                done.WaitUntilSetOrDie(5.Seconds());
            }
        }

        [Test]
        public void RestoresContext()
        {
            using (var adapter = TrackDisposable(new BuiltinContainerAdapter()))
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

                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                    .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                    .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                    .CreateBus()
                    .Start(1);

                adapter.Bus.SendLocal(new SomeMessage { Delay = TimeSpan.FromSeconds(1) });

                done.WaitUntilSetOrDie(5.Seconds());

                result.ShouldBe("asger heller hallas waits no more!");
            }
        }

        [Test]
        public void ContinuesOnAnyWorkerThreadWithContext()
        {
            using (var adapter = TrackDisposable(new BuiltinContainerAdapter()))
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

                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                    .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                    .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                    .CreateBus()
                    .Start(10);

                adapter.Bus.SendLocal(new SomeMessage { Delay = TimeSpan.FromSeconds(1) });

                // wait for a long time, just to be sure some other worker thread will pick it up
                done.WaitUntilSetOrDie(TimeSpan.FromMinutes(1));

                result.ShouldBe("initalfinal");
                initial.ShouldNotBe(final);
                initial.Name.ShouldContain("Rebus 1 worker");
                final.Name.ShouldContain("Rebus 1 worker");
            }
        }

        [Test]
        public void HandlesExceptionsAsUsual()
        {
            using (var adapter = TrackDisposable(new BuiltinContainerAdapter()))
            {
                var done = new ManualResetEvent(false);
                var log = new List<string>();
                var tries = 0;

                adapter.HandleAsync<SomeMessage>(async message =>
                {
                    tries++;
                    Console.WriteLine("Handling");
                    await Task.Yield();
                    throw new Exception("failed");
                });

                Configure.With(adapter)
                    .Logging(l => l.Use(new ListLoggerFactory(log)))
                    .Behavior(b => b.SetMaxRetriesFor<Exception>(3))
                    .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                    .Events(x => x.PoisonMessage += (bus, message, info) => done.Set())
                    .CreateBus()
                    .Start(1);

                adapter.Bus.SendLocal(new SomeMessage());

                done.WaitUntilSetOrDie(1.Seconds());

                log.ShouldContain(x => x.StartsWith("Rebus.Bus.RebusBus|WARN: User exception in Rebus"));
                tries.ShouldBe(3);
            }
        }

        public class SomeMessage
        {
            public TimeSpan Delay { get; set; }
        }
    }
}