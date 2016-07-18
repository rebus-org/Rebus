using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture.NUnit2;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Threading;
using Rebus.Transport;
using Rebus.Workers;
using Rebus.Workers.ThreadBased;

namespace Rebus.Tests.Workers
{
    [TestFixture]
    public class ThreadWorkerUnitTests
    {
        [Test, AutoData]
        public void Dispose_WaitsDefinedTimeout_WhenPendingTasksTakeLonger(string workerName)
        {
            // arrange
            var transport = A.Fake<ITransport>();
            var pipeline = A.Fake<IPipeline>();
            var pipelineInvoker = A.Fake<IPipelineInvoker>();
            var context = A.Fake<ThreadWorkerSynchronizationContext>();
            var manager = A.Fake<ParallelOperationsManager>(fake => fake.WithArgumentsForConstructor(() => new ParallelOperationsManager(1)));
            var backOff = A.Fake<IBackoffStrategy>();
            var logFactory = A.Fake<IRebusLoggerFactory>();
            var log = A.Fake<ILog>();
            var owningBus = A.Fake<RebusBus>();

            var timeout = TimeSpan.FromSeconds(5);

            A.CallTo(() => logFactory.GetCurrentClassLogger())
                .Returns(log);

            // Have stuff to do
            A.CallTo(() => manager.HasPendingTasks)
                .Returns(true); 

            // system under test
            var sut = new ThreadWorker(transport, pipeline, pipelineInvoker, workerName, context, manager, backOff, logFactory, timeout, owningBus);

            // act

            var timer = Stopwatch.StartNew();

            sut.Dispose();

            timer.Stop();

            // assert

            // assuming +- 1 second.
            timer.Elapsed
                .Should().BeCloseTo(timeout, 1000);

            // Must have logs
            A.CallTo(() => 
                log.Warn("Not all async tasks were able to finish within given timeout of {0} seconds!", timeout.TotalSeconds))
                    .MustHaveHappened();

            A.CallTo(() => 
                log.Warn("Worker {0} did not stop withing {1} seconds timeout!", workerName, timeout.TotalSeconds))
                    .MustHaveHappened();

        }

        [Test, AutoData]
        public void Dispose_DoesNotWaitDefinedTimeout_WhenNoPendingTasks(string workerName)
        {
            // arrange
            var transport = A.Fake<ITransport>();
            var pipeline = A.Fake<IPipeline>();
            var pipelineInvoker = A.Fake<IPipelineInvoker>();
            var context = A.Fake<ThreadWorkerSynchronizationContext>();
            var manager = A.Fake<ParallelOperationsManager>(fake => fake.WithArgumentsForConstructor(() => new ParallelOperationsManager(1)));
            var backOff = A.Fake<IBackoffStrategy>();
            var logFactory = A.Fake<IRebusLoggerFactory>();
            var log = A.Fake<ILog>();
            var timeout = TimeSpan.FromSeconds(5);
            var owningBus = A.Fake<RebusBus>();

            A.CallTo(() => logFactory.GetCurrentClassLogger())
                .Returns(log);

            // No stuff to do
            A.CallTo(() => manager.HasPendingTasks)
                .Returns(false);

            // system under test
            var sut = new ThreadWorker(transport, pipeline, pipelineInvoker, workerName, context, manager, backOff, logFactory, timeout, owningBus);

            // act

            var timer = Stopwatch.StartNew();

            sut.Dispose();

            timer.Stop();

            // assert

            // assuming < 1 second.
            timer.Elapsed
                .Should().BeLessOrEqualTo(TimeSpan.FromSeconds(1));

            // NO logs.
            A.CallTo(() =>
                log.Warn("Not all async tasks were able to finish within given timeout of {0} seconds!", timeout.TotalSeconds))
                    .MustNotHaveHappened();

            A.CallTo(() =>
                log.Warn("Worker {0} did not stop withing {1} seconds timeout!", workerName, timeout.TotalSeconds))
                    .MustNotHaveHappened();
        }

        [Test, AutoData]
        public void Dispose_WaitsForTaskToComplete_WhenItTakesLessThanDefinedTimeout(string workerName)
        {
            // arrange
            var transport = A.Fake<ITransport>();
            var pipeline = A.Fake<IPipeline>();
            var pipelineInvoker = A.Fake<IPipelineInvoker>();
            var context = A.Fake<ThreadWorkerSynchronizationContext>();
            var manager = A.Fake<ParallelOperationsManager>(fake => fake.WithArgumentsForConstructor(() => new ParallelOperationsManager(1)));
            var backOff = A.Fake<IBackoffStrategy>();
            var logFactory = A.Fake<IRebusLoggerFactory>();
            var log = A.Fake<ILog>();
            var timeout = TimeSpan.FromSeconds(10);
            var owningBus = A.Fake<RebusBus>();

            A.CallTo(() => logFactory.GetCurrentClassLogger())
                .Returns(log);

            // have stuff to do
            A.CallTo(() => manager.HasPendingTasks)
                .Returns(true);
            
            var taskTakingTime = TimeSpan.FromSeconds(5);

            // wait a bit, then simulate task completion.
            Task.Run(async () =>
            {
                await Task.Delay(taskTakingTime)
                    .ConfigureAwait(true); // forcing sync context for the same thread

                A.CallTo(() => manager.HasPendingTasks)
                    .Returns(false);
            });

            // system under test
            var sut = new ThreadWorker(transport, pipeline, pipelineInvoker, workerName, context, manager, backOff, logFactory, timeout, owningBus);

            // act

            var timer = Stopwatch.StartNew();

            sut.Dispose();

            timer.Stop();

            // assert

            timer.Elapsed
                .Should().BeCloseTo(taskTakingTime, 1000);

            // NO logs.
            A.CallTo(() =>
                log.Warn("Not all async tasks were able to finish within given timeout of {0} seconds!", timeout.TotalSeconds))
                    .MustNotHaveHappened();

            A.CallTo(() =>
                log.Warn("Worker {0} did not stop withing {1} seconds timeout!", workerName, timeout.TotalSeconds))
                    .MustNotHaveHappened();
        }

        [Test, AutoData]
        public async Task Stop_Logs_WhenOperationCanceledExceptionOccuresInTransport(string workerName)
        {
            // arrange
            var transport = A.Fake<ITransport>();
            var pipeline = A.Fake<IPipeline>();
            var pipelineInvoker = A.Fake<IPipelineInvoker>();
            var context = A.Fake<ThreadWorkerSynchronizationContext>();
            var manager = A.Fake<ParallelOperationsManager>(fake => fake.WithArgumentsForConstructor(() => new ParallelOperationsManager(1)));
            var backOff = A.Fake<IBackoffStrategy>();
            var logFactory = A.Fake<IRebusLoggerFactory>();
            var log = A.Fake<ILog>();
            var timeout = TimeSpan.FromSeconds(10);
            var owningBus = A.Fake<RebusBus>();

            A.CallTo(() => logFactory.GetCurrentClassLogger())
                .Returns(log);

            A.CallTo(() => transport.Receive(A<ITransactionContext>._, A<CancellationToken>._))
                .Throws(() => new OperationCanceledException("test"));

            // system under test
            var sut = new ThreadWorker(transport, pipeline, pipelineInvoker, workerName, context, manager, backOff, logFactory, timeout, owningBus);


            // act
            await Task.Delay(TimeSpan.FromSeconds(5));

            sut.Stop();

            // assert
            A.CallTo(() => log.Warn("Execution cancelled."))
                .MustHaveHappened();
        }
    }
}
