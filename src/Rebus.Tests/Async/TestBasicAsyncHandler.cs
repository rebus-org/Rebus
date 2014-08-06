using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports.Msmq;

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

                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                    .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                    .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                    .CreateBus()
                    .Start(1);

                var handlerInstance = new ReplyingHandler(adapter.Bus);
                adapter.Register(() => handlerInstance);

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

        public class ReplyingHandler : IHandleMessagesAsync<SomeMessage>
        {
            readonly IBus bus;

            public ReplyingHandler(IBus bus)
            {
                this.bus = bus;
            }

            public async Task Handle(SomeMessage message)
            {
                var delay = message.Delay;
                await Task.Delay(delay);
                bus.Reply("yo!");
            }
        }


        [Test]
        public void SimpleTest()
        {
            using (var adapter = TrackDisposable(new BuiltinContainerAdapter()))
            {
                var messageHandled = new ManualResetEvent(false);
                var handlerInstance = new AsyncHandler(messageHandled);
                adapter.Register(() => handlerInstance);

                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                    .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                    .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                    .CreateBus()
                    .Start(1);

                adapter.Bus.SendLocal(new SomeMessage { Delay = TimeSpan.FromSeconds(1) });

                messageHandled.WaitUntilSetOrDie(10.Seconds());
            }
        }

        public class AsyncHandler : IHandleMessagesAsync<SomeMessage>
        {
            public readonly List<string> log = new List<string>();

            readonly ManualResetEvent messageHandled;

            public AsyncHandler(ManualResetEvent messageHandled)
            {
                this.messageHandled = messageHandled;
            }

            public async Task Handle(SomeMessage message)
            {
                MessageContext.GetCurrent().Items["test"] = "asger";
                LogAdd(string.Format("Thread: {0}, MessageContext: {1}", Thread.CurrentThread.Name, MessageContext.GetCurrent().Items["test"]));
                await Task.Delay(message.Delay);

                LogAdd(string.Format("Thread: {0}, MessageContext: {1}", Thread.CurrentThread.Name, MessageContext.GetCurrent().Items["test"]));
                await Task.Delay(message.Delay);

                LogAdd(string.Format("Thread: {0}, MessageContext: {1}", Thread.CurrentThread.Name, MessageContext.GetCurrent().Items["test"]));
                await Task.Delay(message.Delay);

                LogAdd(string.Format("Thread: {0}, MessageContext: {1}", Thread.CurrentThread.Name, MessageContext.GetCurrent().Items["test"]));
                messageHandled.Set();
            }

            void LogAdd(string text)
            {
                Console.WriteLine("Add text to log: {0}", text);
                log.Add(text);
            }
        }

        public class SomeMessage
        {
            public TimeSpan Delay { get; set; }
        }
    }
}