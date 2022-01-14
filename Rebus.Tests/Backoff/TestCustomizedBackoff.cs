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

namespace Rebus.Tests.Backoff;

[TestFixture]
public class TestCustomizedBackoff : FixtureBase
{
    [Test]
    public void RunIdleForSomeTime()
    {
        var receiveCalls = RunTest(false);
        var receiveCallsWithCustomizedBackoff = RunTest(true);

        Assert.That(receiveCallsWithCustomizedBackoff, Is.LessThan(receiveCalls),
            "Expected less calls to Receive(...) on the transport because of the customized backoff");
    }

    long RunTest(bool customizeBackoffTimes)
    {
        var items = StartBus(customizeBackoffTimes);
        var activator = items.Item1;
        var countingTransport = items.Item2;

        using (activator)
        {
            Thread.Sleep(5000);
        }

        Console.WriteLine($"5 s idle - #receive: {countingTransport.ReceiveCount}");

        return countingTransport.ReceiveCount;
    }

    Tuple<BuiltinHandlerActivator, CountingTransport> StartBus(bool customizeBackoffTimes)
    {
        var activator = new BuiltinHandlerActivator();
        CountingTransport countingTransport = null;

        Configure.With(activator)
            .Logging(l => l.Console(minLevel: LogLevel.Warn))
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "customized-backoff"))
            .Options(o =>
            {
                o.Decorate<ITransport>(c =>
                {
                    var transport = c.Get<ITransport>();
                    countingTransport = new CountingTransport(transport);
                    return countingTransport;
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

        return Tuple.Create(activator, countingTransport);
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