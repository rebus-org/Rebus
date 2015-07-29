using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.AzureServiceBus;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Tests.Contracts.Transports.Factories;

namespace Rebus.Tests.Async
{
    [TestFixture]
    public class TestAsyncConfigureAwaitBehavior : FixtureBase
    {
        const string QueueName = "testconfigureawait";
        BuiltinContainerAdapter builtinContainerAdapter;
        ConcurrentQueue<string> events;

        protected override void DoSetUp()
        {
            events = new ConcurrentQueue<string>();
            builtinContainerAdapter = new BuiltinContainerAdapter();

            TrackDisposable(builtinContainerAdapter);

            using (var q = new AzureServiceBusMessageQueue(AzureServiceBusMessageQueueFactory.ConnectionString, QueueName))
            {
                q.Purge();
            }

            Configure.With(builtinContainerAdapter)
                .Logging(l => l.Console(minLevel: LogLevel.Info))
                .Transport(t => t.UseAzureServiceBus(AzureServiceBusMessageQueueFactory.ConnectionString, QueueName, "error"))
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
                .CreateBus().Start(1);
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
                WriteEvent(string.Format("context before doing anything: {0}, tx: {1}, current thread: {2}", MessageContext.HasCurrent, TransactionContext.Current, Thread.CurrentThread.Name));

                await Task.Delay(TimeSpan.FromSeconds(3));
                WriteEvent(string.Format("context after first await: {0}, tx: {1}, current thread: {2}", MessageContext.HasCurrent, TransactionContext.Current, Thread.CurrentThread.Name));

                await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                WriteEvent(string.Format("context after ConfigureAwait(false): {0}, tx: {1}, current thread: {2}", MessageContext.HasCurrent, TransactionContext.Current, Thread.CurrentThread.Name));

                done.Set();
            });

            builtinContainerAdapter.Bus.SendLocal("hej med dig!");

            done.WaitUntilSetOrDie(TimeSpan.FromSeconds(10));

            Thread.Sleep(TimeSpan.FromSeconds(1));

            var eventsArray = events.ToArray();

            Console.WriteLine(@"
------------------------------------------------------------------
Got events:

{0}
------------------------------------------------------------------
", string.Join(Environment.NewLine, eventsArray));

            Assert.That(eventsArray, Is.EqualTo(new[]
            {
                "msg context established",

                "uow started",

                "context before doing anything: True, tx: handler tx on thread 'Rebus 1 worker 1', current thread: Rebus 1 worker 1",
                "context after first await: True, tx: handler tx on thread 'Rebus 1 worker 1', current thread: Rebus 1 worker 1",
                "context after ConfigureAwait(false): True, tx: handler tx on thread 'Rebus 1 worker 1', current thread: ",

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