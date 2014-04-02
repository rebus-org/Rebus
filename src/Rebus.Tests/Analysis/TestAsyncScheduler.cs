using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Task = System.Threading.Tasks.Task;

namespace Rebus.Tests.Analysis
{
    [TestFixture]
    public class TestAsyncScheduler
    {
        [Test]
        public async Task VerifyCanTransferThreadBoundThingieToContinuation()
        {
            SynchronizationContext.SetSynchronizationContext(new TestScheduler());

            var list = await AsyncMeth();
            var first = list.First();

            Assert.That(list.All(i => i == first), Is.True, "Not all were the same: {0}", string.Join(", ", list));
            Assert.That(list.All(i => i != -1), Is.True, "Not all were the same: {0}", string.Join(", ", list));
        }

        class TestScheduler : SynchronizationContext
        {
            readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> callbacks = new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();
            readonly Thread worker;

            public TestScheduler()
            {
                worker = new Thread(DoWork) { IsBackground = true };
                worker.Start();
            }
            void DoWork()
            {
                SetSynchronizationContext(this);

                while (true)
                {
                    Tuple<SendOrPostCallback, object> work;

                    if (!callbacks.TryDequeue(out work))
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var item2 = (StateAndMore)work.Item2;
                    try
                    {
                        someThreadBoundValue = item2.ThatInt;
                        work.Item1(item2.OriginalState);
                    }
                    finally
                    {
                        someThreadBoundValue = null;
                    }
                }
            }

            class StateAndMore
            {
                public object OriginalState { get; set; }
                public int ThatInt { get; set; }
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                Console.WriteLine("POST: {0} ({1})", state, Thread.CurrentThread.ManagedThreadId);

                var stateAndMore = new StateAndMore
                {
                    OriginalState = state,
                    ThatInt = someThreadBoundValue.GetValueOrDefault(-1)
                };

                callbacks.Enqueue(Tuple.Create<SendOrPostCallback, object>(d, stateAndMore));
                someThreadBoundValue = null;
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                Console.WriteLine("SEND: {0} ({1})", state, Thread.CurrentThread.ManagedThreadId);
                base.Send(d, state);
            }

            public override void OperationStarted()
            {
                Console.WriteLine("STARTED ({0})", Thread.CurrentThread.ManagedThreadId);
                base.OperationStarted();
            }

            public override void OperationCompleted()
            {
                Console.WriteLine("COMPLETED ({0})", Thread.CurrentThread.ManagedThreadId);
                base.OperationCompleted();
            }
        }

        static int counter = 1;

        [ThreadStatic]
        static int? someThreadBoundValue;

        static void AddThreadId(List<int> list)
        {
            if (!someThreadBoundValue.HasValue)
            {
                someThreadBoundValue = GetNextValue();
                Console.WriteLine("Value {0} bound to thread {1}", someThreadBoundValue, Thread.CurrentThread.ManagedThreadId);
            }

            list.Add(someThreadBoundValue.Value);
        }

        static int GetNextValue()
        {
            return Interlocked.Increment(ref counter);
        }

        async Task<List<int>> AsyncMeth()
        {
            var list = new List<int>();

            for (var lol = 0; lol < 10; lol++)
            {
                await Task.Delay(lol * 10 + 10);
                
                AddThreadId(list);
            }

            return list;
        }
    }
}