using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Tests.Extensions;
using Rebus.Transport;
using Rebus.Transport.InMem;
using Rebus.Workers;
using Rebus.Workers.ThreadPoolBased;

#pragma warning disable 1998

namespace Rebus.Tests.Workers
{
    [TestFixture]
    public class TestThreadPoolBasedWorkers : FixtureBase
    {
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = Using(new BuiltinHandlerActivator());

            Configure.With(_activator)
                .Logging(l => l.Use(new ConsoleLoggerFactory(false)
                {
                    Filters =
                    {
                        //logStatement => logStatement.Level >= LogLevel.Warn
                        //                || logStatement.Type.FullName.Contains("ThreadPoolWorker")
                    }
                }))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "threadpool-workers-test"))
                .Options(o =>
                {
                    o.UseThreadPoolMessageDispatch();

                    //o.Register<IWorkerFactory>(c =>
                    //{
                    //    var transport = c.Get<ITransport>();
                    //    var loggerFactory = c.Get<IRebusLoggerFactory>();
                    //    var pipeline = c.Get<IPipeline>();
                    //    var pipelineInvoker = c.Get<IPipelineInvoker>();
                    //    var options = c.Get<Options>();
                    //    return new ThreadPoolWorkerFactory(transport, loggerFactory, pipeline, pipelineInvoker, options, c.Get<RebusBus>);
                    //});

                    o.SetNumberOfWorkers(0);
                    o.SetMaxParallelism(1);
                })
                .Start();
        }

        [TestCase(4)]
        public async Task CanReceiveSomeMessages(int messageCount)
        {
            var counter = new SharedCounter(messageCount);
            
            _activator.Handle<string>(async message =>
            {
                Console.WriteLine($"Handling message: {message}");
                counter.Decrement();
            });

            await Task.WhenAll(Enumerable.Range(0, messageCount)
                .Select(i => _activator.Bus.SendLocal($"This is message {i}")));

            _activator.Bus.Advanced.Workers.SetNumberOfWorkers(1);

            counter.WaitForResetEvent(100);
        }
    }
}