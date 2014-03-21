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
        public void SimpleTest()
        {
            using (var adapter = TrackDisposable(new BuiltinContainerAdapter()))
            {
                var messageHandled = new ManualResetEvent(false);
                var handlerInstance = new AsyncHandler(messageHandled);
                adapter.Register(() => handlerInstance);

                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel:LogLevel.Warn))
                    .Behavior(b => b.SetMaxRetriesFor<Exception>(0))
                    .Transport(t => t.UseMsmq(InputQueue, ErrorQueue))
                    .CreateBus()
                    .Start();

                adapter.Bus.SendLocal(new SomeMessage {Delay = TimeSpan.FromSeconds(1)});

                messageHandled.WaitUntilSetOrDie(4.Seconds());
            }
        }

        public class AsyncHandler : IHandleMessagesAsync<SomeMessage>
        {
            readonly ManualResetEvent messageHandled;
            public readonly List<string> Log = new List<string>();

            public AsyncHandler(ManualResetEvent messageHandled)
            {
                this.messageHandled = messageHandled;
            }

            public async Task Handle(SomeMessage message)
            {
                Log.Add(string.Format("Thread: {0}", Thread.CurrentThread.Name));

                await Task.Delay(message.Delay);

                Log.Add(string.Format("Thread: {0}", Thread.CurrentThread.Name));

                await Task.Delay(message.Delay);

                Log.Add(string.Format("Thread: {0}", Thread.CurrentThread.Name));

                await Task.Delay(message.Delay);

                Log.Add(string.Format("Thread: {0}", Thread.CurrentThread.Name));

                messageHandled.Set();
            }
        }

        public class SomeMessage
        {
            public TimeSpan Delay { get; set; }
        }
    }
}