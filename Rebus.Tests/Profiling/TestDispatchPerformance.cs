using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Profiling;
using Rebus.Serialization;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Profiling
{
    public class TestDispatchPerformance : FixtureBase
    {
        [Theory]
        [InlineData(10000, 20)]
        public void TakeTime(int numberOfMessages, int numberOfSamples)
        {
            var profilerStats = new PipelineStepProfilerStats();

            var results = Enumerable.Range(1, numberOfSamples)
                .Select(i =>
                {
                    Console.Write($"Performing sample {i}: ");
                    var result = RunTest(numberOfMessages, profilerStats);
                    Console.WriteLine($"{result.TotalSeconds:0.#####}");
                    return result;
                })
                .Select(t => t.TotalSeconds)
                .ToList();

            Console.WriteLine($@"{numberOfSamples} runs
Avg s: {results.Average():0.00###}
Avg msg/s: {numberOfMessages * numberOfSamples / results.Sum():0}

Stats:
{string.Join(Environment.NewLine, profilerStats.GetAndResetStats().Select(s => $"    {s}"))}");
        }

        static TimeSpan RunTest(int numberOfMessages, PipelineStepProfilerStats profilerStats)
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
                    })
                    .Start();

                var serializer = new JsonSerializer();
                var boy = new SomeMessage("hello there!");

                numberOfMessages.Times(() =>
                {
                    var headers = new Dictionary<string, string> { { Headers.MessageId, Guid.NewGuid().ToString() } };
                    var message = new Message(headers, boy);
                    var transportMessage = serializer.Serialize(message).Result;
                    var inMemTransportMessage = transportMessage.ToInMemTransportMessage();

                    network.Deliver("perftest", inMemTransportMessage);
                });


                var numberOfReceivedMessages = 0;
                var gotAllMessages = new ManualResetEvent(false);

                adapter.Handle<SomeMessage>(async m =>
                {
                    numberOfReceivedMessages++;
                    if (numberOfReceivedMessages == numberOfMessages)
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