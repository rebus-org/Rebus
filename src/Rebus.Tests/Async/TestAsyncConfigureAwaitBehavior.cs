using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Tests.Async
{
    [TestFixture]
    public class TestAsyncConfigureAwaitBehavior : FixtureBase
    {
        const string QueueName = "test.configureawait";
        BuiltinContainerAdapter builtinContainerAdapter;
        ConcurrentQueue<string> events;

        protected override void DoSetUp()
        {
            events = new ConcurrentQueue<string>();
            builtinContainerAdapter = new BuiltinContainerAdapter();

            TrackDisposable(builtinContainerAdapter);

            Configure.With(builtinContainerAdapter)
                .Logging(l => l.Console(minLevel: LogLevel.Info))
                .Transport(t => t.UseMsmq(QueueName, "error"))
                .Events(e =>
                {
                    e.MessageContextEstablished += (bus, context) =>
                    {
                        WriteEvent("msg context established");

                        context.Disposed += () =>
                        {
                            WriteEvent("msg context disposed");
                        };
                    };

                    e.AddUnitOfWorkManager(new EventOutputtingUnitOfWorkManager(text => WriteEvent(text)));
                })
                .CreateBus().Start();
        }

        protected override void DoTearDown()
        {
            MsmqUtil.Delete(QueueName);
        }

        [Test]
        public void SequenceOfEventsIsRightAndMessageContextIsAvailableAsItShouldBe()
        {
            var done = new ManualResetEvent(false);

            builtinContainerAdapter.HandleAsync<string>(async str =>
            {
                WriteEvent(string.Format("context before doing anything: {0}", MessageContext.HasCurrent));

                await Task.Delay(TimeSpan.FromSeconds(1));
                WriteEvent(string.Format("context after first await: {0}", MessageContext.HasCurrent));

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                WriteEvent(string.Format("context after ConfigureAwait(false): {0}", MessageContext.HasCurrent));

                done.Set();
            });

            builtinContainerAdapter.Bus.SendLocal("hej med dig!");

            done.WaitUntilSetOrDie(TimeSpan.FromSeconds(5));

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var eventsArray = events.ToArray();

            Console.WriteLine(@"Got events:

{0}
", string.Join(Environment.NewLine, eventsArray));

            Assert.That(eventsArray, Is.EqualTo(new[]
            {
                "msg context established",

                "uow started",

                "context before doing anything: True",
                "context after first await: True",
                "context after ConfigureAwait(false): True",

                "uow commit",
                "uow dispose",

                "msg context disposed"
            }));
        }

        void WriteEvent(string message, params object[] objs)
        {
            Console.WriteLine(message, objs);
            events.Enqueue(string.Format(message, objs));
        }
    }

    public class EventOutputtingUnitOfWorkManager : IUnitOfWorkManager
    {
        readonly Action<string> outputEvent;

        public EventOutputtingUnitOfWorkManager(Action<string> outputEvent)
        {
            this.outputEvent = outputEvent;
        }

        public IUnitOfWork Create()
        {
            return new EventOutputtingUnitOfWork(outputEvent);
        }

        class EventOutputtingUnitOfWork : IUnitOfWork
        {
            readonly Action<string> outputEvent;

            public EventOutputtingUnitOfWork(Action<string> outputEvent)
            {
                this.outputEvent = outputEvent;

                outputEvent("uow started");
            }

            public void Commit()
            {
                outputEvent("uow commit");
            }

            public void Abort()
            {
                outputEvent("uow abort");
            }

            public void Dispose()
            {
                outputEvent("uow dispose");
            }
        }
    }
}