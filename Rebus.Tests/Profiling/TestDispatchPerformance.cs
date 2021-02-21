using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Pipeline.Invokers;
using Rebus.Profiling;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
#pragma warning disable 1998

namespace Rebus.Tests.Profiling
{
    [TestFixture]
    public class TestDispatchPerformance : FixtureBase
    {
        public enum PipelineInvokerMode
        {
            Default,
            DefaultNew,
            Action,
        }

        [TestCase(100000, 10, PipelineInvokerMode.Action, Ignore = "Irrelevant for now")]
        [TestCase(100000, 10, PipelineInvokerMode.Default, Ignore = "Irrelevant for now")]
        [TestCase(100000, 10, PipelineInvokerMode.DefaultNew)]
        public void TakeTime(int numberOfMessages, int numberOfSamples, PipelineInvokerMode pipelineInvokerMode)
        {
            Console.WriteLine($"Running {numberOfSamples} samples with {numberOfMessages} msgs and mode {pipelineInvokerMode}");

            var profilerStats = new PipelineStepProfilerStats();

            var results = Enumerable.Range(1, numberOfSamples)
                .Select(i =>
                {
                    Console.Write($"Performing sample {i}: ");
                    var result = RunTest(numberOfMessages, profilerStats, pipelineInvokerMode);
                    Console.WriteLine($"{result.TotalSeconds:0.#####}");
                    return result;
                })
                .Select(t => t.TotalSeconds)
                .ToList();

            Console.WriteLine($@"{numberOfSamples} runs
Avg s: {results.Average():0.00###}
Avg msg/s: {numberOfMessages / results.Average():0}

Med s: {results.Median():0.00###}
Med msg/s: {numberOfMessages / results.Median():0}

Pipeline invoker: {pipelineInvokerMode}

Stats:
{string.Join(Environment.NewLine, profilerStats.GetAndResetStats().Select(s => $"    {s}"))}");
        }

        static TimeSpan RunTest(int numberOfMessages, PipelineStepProfilerStats profilerStats, PipelineInvokerMode pipelineInvokerMode)
        {
            using (var adapter = new BuiltinHandlerActivator())
            {
                var network = new InMemNetwork();

                Configure.With(adapter)
                    .Logging(l => l.Console(LogLevel.Warn))
                    .Transport(t => t.UseInMemoryTransport(network, "perftest"))
                    .Options(o =>
                    {
                        o.SetNumberOfWorkers(0);
                        o.SetMaxParallelism(1);

                        o.Decorate<IPipeline>(c => new PipelineStepProfiler(c.Get<IPipeline>(), profilerStats));

                        switch (pipelineInvokerMode)
                        {
                            case PipelineInvokerMode.Default:
                                o.Register<IPipelineInvoker>(c => new DefaultPipelineInvoker(c.Get<IPipeline>()));
                                break;
                            case PipelineInvokerMode.DefaultNew:
                                o.Register<IPipelineInvoker>(c => new DefaultPipelineInvokerNew(c.Get<IPipeline>()));
                                break;
                            case PipelineInvokerMode.Action:
                                o.Register<IPipelineInvoker>(c => new ActionPipelineInvoker(c.Get<IPipeline>()));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException($"Unknown pipeline invoker: {pipelineInvokerMode}");
                        }
                    })
                    .Start();

                var serializer = new JsonSerializer(new SimpleAssemblyQualifiedMessageTypeNameConvention());
                var boy = new SomeMessage("hello there!");

                for(var counter = 0; counter < numberOfMessages; counter++)
                {
                    var headers = new Dictionary<string, string> { { Headers.MessageId, Guid.NewGuid().ToString() } };
                    var message = new Message(headers, boy);
                    var transportMessage = serializer.Serialize(message).Result;
                    var inMemTransportMessage = transportMessage.ToInMemTransportMessage();

                    network.Deliver("perftest", inMemTransportMessage);
                };

                var numberOfReceivedMessages = 0;
                var gotAllMessages = new ManualResetEvent(false);

                adapter.Handle<SomeMessage>(async m =>
                {
                    Interlocked.Increment(ref numberOfReceivedMessages);

                    if (Volatile.Read(ref numberOfReceivedMessages) == numberOfMessages)
                    {
                        gotAllMessages.Set();
                    }
                });

                var stopwatch = Stopwatch.StartNew();

                adapter.Bus.Advanced.Workers.SetNumberOfWorkers(1);
                gotAllMessages.WaitOrDie(TimeSpan.FromSeconds(30));

                return stopwatch.Elapsed;
            }
        }

        class SomeMessage
        {
            public SomeMessage(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }
    }
}