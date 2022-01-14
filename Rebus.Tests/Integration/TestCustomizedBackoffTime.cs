using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Backoff;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Tests.Transport;
using Rebus.Transport;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Integration;

[TestFixture]
public class TestCustomizedBackoffTime : FixtureBase
{
    BuiltinHandlerActivator _activator;
    RebusConfigurer _rebusConfigurer;
    ConcurrentQueue<double> _waitedSeconds;

    DateTime _busStartTime;

    protected override void SetUp()
    {
        _activator = Using(new BuiltinHandlerActivator());

        _waitedSeconds = new ConcurrentQueue<double>();

        _rebusConfigurer = Configure.With(_activator)
            .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "test backoff"))
            .Options(o =>
            {
                o.SetBackoffTimes(TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1));

                o.Decorate<ITransport>(c =>
                {
                    var transport = c.Get<ITransport>();
                    var transportTap = new TransportTap(transport);

                    transportTap.NoMessageReceived += () =>
                    {
                        var elapsedSinceStart = DateTime.UtcNow - _busStartTime;
                        var elapsedSeconds = Math.Round(elapsedSinceStart.TotalSeconds, 1);
                        _waitedSeconds.Enqueue(elapsedSeconds);
                    };

                    return transportTap;
                });

                o.SetMaxParallelism(10);
                o.SetNumberOfWorkers(1);
            });
    }

    [Test]
    public async Task ItWorks()
    {
        _busStartTime = DateTime.UtcNow;
        _rebusConfigurer.Start();

        await Task.Delay(TimeSpan.FromSeconds(5));

        _activator.Dispose();

        Console.WriteLine(@"Receive attempts:
{0}

Diffs:
{1}", string.Join(Environment.NewLine, _waitedSeconds),
            string.Join(Environment.NewLine, GetDiffs(_waitedSeconds)));
    }

    static IEnumerable<double> GetDiffs(IEnumerable<double> waitedSeconds)
    {
        var list = waitedSeconds.ToList();

        for (int index1 = 0, index2 = 1; index2 < list.Count; index1++, index2++)
        {
            var time1 = list[index1];
            var time2 = list[index2];

            yield return time2 - time1;
        }
    }
}