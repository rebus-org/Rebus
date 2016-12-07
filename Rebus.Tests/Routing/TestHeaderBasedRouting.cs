using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Routing.TransportMessages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Routing
{
    public class TestHeaderBasedRouting : FixtureBase
    {
        const string SpecialHeaderKey = "forward";

        readonly ConcurrentQueue<DoneWork> _doneWork;
        readonly InMemNetwork _network;

        readonly Dictionary<string, string> _forwardHeaders = new Dictionary<string, string>
        {
            {SpecialHeaderKey, ""}
        };

        public TestHeaderBasedRouting()
        {
            _doneWork = new ConcurrentQueue<DoneWork>();
            _network = new InMemNetwork();
        }

        [Theory]
        [InlineData(100)]
        public async Task CanDistributeWork(int numberOfMessages)
        {
            var transportConfigurer = GetTransportConfigurer();

            var workers = new[] {"worker1", "worker2", "worker3", "worker4"}
                .Select(TestConfig.GetName)
                .ToArray();

            var distributorQueueName = TestConfig.GetName("distributor");

            workers.ForEach(name =>
            {
                StartWorker(name, transportConfigurer);
            });

            var distributor = Configure.With(Using(new BuiltinHandlerActivator()))
                .Logging(l => l.Console(LogLevel.Info))
                .Transport(t =>
                {
                    transportConfigurer(t, distributorQueueName);
                })
                .Routing(r =>
                {
                    var nextDestinationIndex = 0;

                    r.AddTransportMessageForwarder(async transportMessage =>
                    {
                        var headers = transportMessage.Headers;

                        if (headers.ContainsKey(SpecialHeaderKey))
                        {
                            var index = Interlocked.Increment(ref nextDestinationIndex) % workers.Length;

                            return ForwardAction.ForwardTo(workers[index]);
                        }

                        return ForwardAction.None;
                    });
                })
                .Options(o =>
                {
                    o.LogPipeline(verbose: true);
                })
                .Start();
            
            await Task.WhenAll(
                Enumerable.Range(0, numberOfMessages)
                    .Select(id => new Work { WorkId = id })
                    .Select(work => distributor.SendLocal(work, _forwardHeaders))
                );

            await _doneWork.WaitUntil(w => w.Count == numberOfMessages, timeoutSeconds: 20);

            var workByWorker = _doneWork.GroupBy(w => w.Worker).ToList();

            Console.WriteLine(@"Done work:

{0}

", string.Join(Environment.NewLine, workByWorker.Select(g => $"    {g.Key}: {g.Count()}")));

            Assert.Equal(workers.Length, workByWorker.Count);
        }

        Action<StandardConfigurer<ITransport>, string> GetTransportConfigurer()
        {
            return (configurer, queueName) =>
            {
                configurer.UseInMemoryTransport(_network, queueName);
            };
        }

        void StartWorker(string queueName, Action<StandardConfigurer<ITransport>, string> transportConfigurer)
        {
            var handlerActivator = Using(new BuiltinHandlerActivator());

            handlerActivator.Handle<Work>(async work =>
            {
                _doneWork.Enqueue(new DoneWork { Work = work, Worker = queueName });
            });

            Configure.With(handlerActivator)
                .Transport(t =>
                {
                    transportConfigurer(t, queueName);
                })
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);
                })
                .Start();
        }

        class Work
        {
            public int WorkId { get; set; }
        }

        class DoneWork
        {
            public Work Work { get; set; }
            public string Worker { get; set; }
        }
    }
}