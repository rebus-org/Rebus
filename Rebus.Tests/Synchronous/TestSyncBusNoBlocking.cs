using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Synchronous;

[TestFixture]
[Description(@"
Verifies that a single-threaded environment is NOT deadlocked when using the sync API of Rebus.

The single-threaded environment emulates the behavior of ASP.NET (which insists on marshalling continuations
back to the originating thread) and WPF (the UI thread, of which there is only one) by starting a worker
thread and giving it a custom synchronization context. This custom synchronization context simply queues
continuations and executes them as they become available.

The trick then is to Post to that synchronization context a function that uses Rebus' sync API, e.g. to

    bus.SendLocal('whatever'); // void

which would deadlock the calling thread if the operation was implemented simply by doing this:

    bus.SendLocal('whatever').Wait(); // Task

The fact that this test passes verifies that a synchronous operation can be performed by using the real,
async implementation underneath the covers without deadlocking, even in a single-threaded environment.
")]
public class TestSyncBusNoBlocking : FixtureBase
{
    BuiltinHandlerActivator _activator;

    protected override void SetUp()
    {
        _activator = new BuiltinHandlerActivator();

        Using(_activator);

        Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "sync-bus-tjek"))
            .Options(o =>
            {
                o.Decorate<ITransport>(c => new DelayOperationsTransportDecorator(c.Get<ITransport>()));
                o.SetNumberOfWorkers(1);
                o.SetMaxParallelism(1);
            })
            .Start();
    }

    [Test]
    public void DoesNotBlockOnSend()
    {
        var bus = _activator.Bus.Advanced.SyncBus;
        var gotMessage = new ManualResetEvent(false);

        _activator.AddHandlerWithBusTemporarilyStopped<string>(async str => gotMessage.Set());

        using (var aspNet = new AspNetSimulatorSynchronizationContext())
        {
            aspNet.Post(s =>
            {
                bus.SendLocal("HEJ MED DIG MIN VEN");
            }, null);

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(3));
        }
    }

    [Test]
    public void DoesNotBlockOnCompletingTransactionContext()
    {
        var bus = _activator.Bus.Advanced.SyncBus;
        var gotMessage = new ManualResetEvent(false);

        _activator.AddHandlerWithBusTemporarilyStopped<string>(async str => gotMessage.Set());

        using (var aspNet = new AspNetSimulatorSynchronizationContext())
        {
            aspNet.Post(s =>
            {
                using (var scope = new RebusTransactionScope())
                {
                    try
                    {
                        // enlist some other async thing
                        scope.TransactionContext.OnCommit(async _ =>
                        {
                            Console.WriteLine("waiting....");
                            await Task.Delay(100);
                            Console.WriteLine("waiting....");
                            await Task.Delay(100);
                            Console.WriteLine("waiting....");
                            await Task.Delay(100);
                        });

                        // enlist an operation in the context
                        bus.SendLocal("HEJ MED DIG MIN VEN");

                        scope.Complete();
                    }
                    finally
                    {
                        AmbientTransactionContext.SetCurrent(null);
                    }
                }
            }, null);

            gotMessage.WaitOrDie(TimeSpan.FromSeconds(3));
        }
    }

    class AspNetSimulatorSynchronizationContext : SynchronizationContext, IDisposable
    {
        readonly ConcurrentQueue<Tuple<SendOrPostCallback, object>> _continuations = new ConcurrentQueue<Tuple<SendOrPostCallback, object>>();
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        readonly Thread _workerThread;

        ExceptionDispatchInfo _caughtException;

        public AspNetSimulatorSynchronizationContext()
        {
            Console.WriteLine("Starting ASP.NET(-simulating) message loop...");

            _workerThread = new Thread(Run) { IsBackground = true };
            _workerThread.Start();
        }

        public override void Post(SendOrPostCallback function, object state)
        {
            _continuations.Enqueue(Tuple.Create(function, state));
        }

        void Run()
        {
            SetSynchronizationContext(this);

            Console.WriteLine("Message loop started");

            try
            {
                var cancellationToken = _cancellationTokenSource.Token;

                while (!cancellationToken.IsCancellationRequested)
                {
                    Tuple<SendOrPostCallback, object> item;

                    if (!_continuations.TryDequeue(out item))
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    Console.WriteLine($"Executing {item.Item1}({item.Item2}) on {Thread.CurrentThread.ManagedThreadId}");
                    item.Item1(item.Item2);
                }
            }
            catch (Exception exception)
            {
                _caughtException = ExceptionDispatchInfo.Capture(exception);
            }
            finally
            {
                Console.WriteLine("Message loop stopped");
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Stopping message loop...");

            _cancellationTokenSource?.Cancel();

            if (!_workerThread.Join(1000))
            {
                Console.WriteLine("Worker thread did not stop within 1 s");
            }

            _caughtException?.Throw();
        }
    }

    /// <summary>
    /// Decorator that ensures that all bus operations result in continuations
    /// </summary>
    class DelayOperationsTransportDecorator : ITransport
    {
        readonly ITransport _transport;

        public DelayOperationsTransportDecorator(ITransport transport)
        {
            _transport = transport;
        }

        public async Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            for (var counter = 0; counter < 10; counter++)
            {
                Console.Write(".");
                await Task.Delay(100);
            }

            Console.WriteLine($"Sending {message.GetMessageLabel()}");

            await _transport.Send(destinationAddress, message, context);
        }

        public async Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            return await _transport.Receive(context, cancellationToken);
        }

        public void CreateQueue(string address) => _transport.CreateQueue(address);

        public string Address => _transport.Address;
    }
}