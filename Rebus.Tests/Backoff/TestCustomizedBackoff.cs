using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Backoff;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Backoff
{
    [TestFixture]
    public class TestCustomizedBackoff : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        CountingTransport _countingTransport;

        [TestCase(true)]
        [TestCase(false)]
        public void RunIdleForSomeTIme(bool customizeBackoffTimes)
        {
            StartBus(customizeBackoffTimes);

            Thread.Sleep(5000);

            CleanUpDisposables();

            Console.WriteLine($"5 s idle - #receive: {_countingTransport.ReceiveCount}");
        }

        void StartBus(bool customizeBackoffTimes)
        {
            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Logging(l => l.Console(minLevel: LogLevel.Warn))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "customized-backoff"))
                .Options(o =>
                {
                    o.Decorate<ITransport>(c =>
                    {
                        var transport = c.Get<ITransport>();
                        _countingTransport = new CountingTransport(transport);
                        return _countingTransport;
                    });

                    o.SetNumberOfWorkers(20);
                    o.SetMaxParallelism(20);

                    if (customizeBackoffTimes)
                    {
                        o.SetBackoffTimes(
                            TimeSpan.FromMilliseconds(100),
                            TimeSpan.FromMilliseconds(500),
                            TimeSpan.FromSeconds(5));
                    }
                })
                .Start();
        }

        class CountingTransport : ITransport
        {
            public long ReceiveCount;

            readonly ITransport _transport;

            public CountingTransport(ITransport transport)
            {
                _transport = transport;
            }
            public void CreateQueue(string address)
            {
                _transport.CreateQueue(address);
            }

            public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
            {
                return _transport.Send(destinationAddress, message, context);
            }

            public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref ReceiveCount);

                return _transport.Receive(context, cancellationToken);
            }

            public string Address => _transport.Address;
        }
    }
}