using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Workers;

[TestFixture]
public class ThreadWorkerIntegrationTests : FixtureBase
{
    static readonly string InputQueueName = TestConfig.GetName("test.async.input");
    IBus _bus;
    BuiltinHandlerActivator _handlerActivator;

    protected override void SetUp()
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

    [Test]
    public async Task Dispose_WaitsDefinedTimeout_WhenPendingTasksTakeLonger()
    {
        // arrange
        var startedHandle = new ManualResetEvent(false);

        _handlerActivator.AddHandlerWithBusTemporarilyStopped<string>(async str =>
        {
            startedHandle.Set();

            await Task.Delay(TimeSpan.FromSeconds(10));
        });

        // act
        await _bus.Send("something");

        startedHandle.WaitOne();
           
        var timer = Stopwatch.StartNew();
            
        _bus.Dispose();

        timer.Stop();

        // assert

        Assert.That(timer.Elapsed, Is.GreaterThan(TimeSpan.FromSeconds(4)).And.LessThan(TimeSpan.FromSeconds(6)));
    }

    [Test]
    public async Task Dispose_DoesNotWaitDefinedTimeout_WhenNoPendingTasks()
    {
        // arrange
        var finishedHandle = new ManualResetEvent(false);

        _handlerActivator.AddHandlerWithBusTemporarilyStopped<string>(str =>
        {
            finishedHandle.Set();
            return Task.FromResult(0);
        });

        // act
        await _bus.Send("something");


                
        finishedHandle.WaitOne();

        var timer = Stopwatch.StartNew();

        _bus.Dispose();

        timer.Stop();

        // assert
        Assert.That(timer.Elapsed, Is.LessThan(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task Dispose_WaitsForTaskToComplete_WhenItTakesLessThanDefinedTimeout()
    {
        // arrange
        var startedHandle = new ManualResetEvent(false);
        var taskTakingTime = TimeSpan.FromSeconds(5);

        _handlerActivator.AddHandlerWithBusTemporarilyStopped<string>(async str =>
        {
            startedHandle.Set();

            await Task.Delay(taskTakingTime);
        });

        // act
        await _bus.Send("something");

        startedHandle.WaitOne();

        var timer = Stopwatch.StartNew();

        _bus.Dispose();

        timer.Stop();

        // assert

        var tolerance = TimeSpan.FromSeconds(1);

        Assert.That(timer.Elapsed, Is.GreaterThan(taskTakingTime.Subtract(tolerance)).And.LessThan(taskTakingTime.Add(tolerance)));
    }
}