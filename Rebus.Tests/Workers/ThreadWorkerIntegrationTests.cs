using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

namespace Rebus.Tests.Workers
{
    public class ThreadWorkerIntegrationTests : FixtureBase
    {
        static readonly string InputQueueName = TestConfig.GetName("test.async.input");
        readonly IBus _bus;
        readonly BuiltinHandlerActivator _handlerActivator;

        public ThreadWorkerIntegrationTests()
        {
            _handlerActivator = new BuiltinHandlerActivator();

            _bus = Configure.With(_handlerActivator)
                .Routing(r => r.TypeBased().Map<string>(InputQueueName))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), InputQueueName))
                .Options(o => o.SetNumberOfWorkers(1))
                .Logging(l => l.Trace())
                .Options(o => o.SetWorkerShutdownTimeout(TimeSpan.FromSeconds(5))) // new option we are testing against
                .Start();

            Using(_bus);
        }

        [Fact]
        public async Task Dispose_WaitsDefinedTimeout_WhenPendingTasksTakeLonger()
        {
            // arrange
            var startedHandle = new ManualResetEvent(false);

            _handlerActivator.Handle<string>(async str =>
            {
                startedHandle.Set();

                await Task.Delay(TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);
            });

            // act
            await _bus.Send("something")
                .ConfigureAwait(false);

            startedHandle.WaitOne();
           
            var timer = Stopwatch.StartNew();
            
            _bus.Dispose();

            timer.Stop();

            // assert

            timer.Elapsed
                .Should().BeCloseTo(TimeSpan.FromSeconds(5), 1000);
        }

        [Fact]
        public async Task Dispose_DoesNotWaitDefinedTimeout_WhenNoPendingTasks()
        {
            // arrange
            var finishedHandle = new ManualResetEvent(false);

            _handlerActivator.Handle<string>(str =>
            {
                finishedHandle.Set();
                return Task.FromResult(0);
            });

            // act
            await _bus.Send("something")
                .ConfigureAwait(false);

            finishedHandle.WaitOne();

            var timer = Stopwatch.StartNew();

            _bus.Dispose();

            timer.Stop();

            // assert

            timer.Elapsed
                .Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Dispose_WaitsForTaskToComplete_WhenItTakesLessThanDefinedTimeout()
        {
            // arrange
            var startedHandle = new ManualResetEvent(false);
            var taskTakingTime = TimeSpan.FromSeconds(5);

            _handlerActivator.Handle<string>(async str =>
            {
                startedHandle.Set();

                await Task.Delay(taskTakingTime)
                    .ConfigureAwait(false);
            });

            // act
            await _bus.Send("something")
                .ConfigureAwait(false);

            startedHandle.WaitOne();

            var timer = Stopwatch.StartNew();

            _bus.Dispose();

            timer.Stop();

            // assert

            timer.Elapsed
                .Should().BeCloseTo(taskTakingTime, 1000);
        }
    }
}