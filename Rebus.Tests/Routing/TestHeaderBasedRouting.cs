using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Routing.TransportMessages;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Routing
{
    [TestFixture]
    public class TestHeaderBasedRouting : FixtureBase
    {
        const string SpecialHeaderKey = "forward";

        ConcurrentQueue<DoneWork> _doneWork;
        InMemNetwork _network;

        readonly Dictionary<string, string> _forwardHeaders = new Dictionary<string, string>
        {
            {SpecialHeaderKey, ""}
        };

        protected override void SetUp()
        {
            _doneWork = new ConcurrentQueue<DoneWork>();
            _network = new InMemNetwork();
        }

        [TestCase("inmem", 100)]
        [TestCase("msmq", 100)]
        public async Task CanDistributeWork(string transportType, int numberOfMessages)
        {
            Console.WriteLine("TRANSPORT: {0}", transportType);
            Console.WriteLine();

            var transportConfigurer = GetTransportConfigurer(transportType);

            var workers = new[] {"worker1", "worker2", "worker3", "worker4"}
                .Select(TestConfig.QueueName)
                .ToArray();

            var distributorQueueName = TestConfig.QueueName("distributor");

            if (transportType == "msmq")
            {
                MsmqUtil.Delete(distributorQueueName);
                workers.ForEach(MsmqUtil.Delete);
            }

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

            await _doneWork.WaitUntil(w => w.Count == numberOfMessages, timeoutSeconds: 10);

            var workByWorker = _doneWork.GroupBy(w => w.Worker).ToList();

            Console.WriteLine(@"Done work:

{0}

", string.Join(Environment.NewLine, workByWorker.Select(g => string.Format("    {0}: {1}", g.Key, g.Count()))));

            Assert.That(workByWorker.Count, Is.EqualTo(workers.Length), "Expected that all workers got to do some work!");
        }

        Action<StandardConfigurer<ITransport>, string> GetTransportConfigurer(string transportType)
        {
            switch (transportType)
            {
                case "inmem":
                    return (configurer, queueName) =>
                    {
                        configurer.UseInMemoryTransport(_network, queueName);
                    };

                case "msmq":
                    return (configurer, queueName) =>
                    {
                        configurer.UseMsmq(queueName);
                    };

                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unknown transport type: {0}", transportType));
            }
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
                .Options(o => o.SetNumberOfWorkers(1).SetMaxParallelism(1))
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