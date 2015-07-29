using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.MsmqLoadBalancer;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.LoadBalancer
{
    [TestFixture]
    public class TestMsmqLoadBalancer : FixtureBase, IDetermineMessageOwnership
    {
        const string LoadBalancerInputQueueName = "test.loadbalancer.input";
        LoadBalancerService service;

        List<string> queuesToReset;

        protected override void DoSetUp()
        {
            queuesToReset = new List<string> {LoadBalancerInputQueueName};

            try
            {
                MsmqUtil.PurgeQueue(LoadBalancerInputQueueName);
            }
            catch { }

            service = new LoadBalancerService(LoadBalancerInputQueueName);
            
            TrackDisposable(service);
        }

        protected override void DoTearDown()
        {
            CleanUpTrackedDisposables();
            
            queuesToReset.ForEach(MsmqUtil.Delete);
        }

        [Test]
        public void CannotStartWithoutAddingAtLeastOneWorker()
        {
            Assert.Throws<InvalidOperationException>(() => service.Start());
        }

        [TestCase(10, 1)]
        [TestCase(100, 5)]
        [TestCase(2000, 10, Ignore = true)]
        public void CanDistributeWorkAmongAddedWorkers(int numberOfMessages, int numberOfWorkerEndpoints)
        {
            var workerQueueNames = Enumerable
                .Range(1, numberOfWorkerEndpoints)
                .Select(workerNumber => string.Format("test.loadbalancer.worker.{0:00}", workerNumber))
                .ToList();

            Console.WriteLine(@"Load balancer test running - will send {0} messages to load balancer configured with endpoints:

{1}

",
                numberOfMessages, string.Join(Environment.NewLine, workerQueueNames.Select(name => "    " + name)));

            var workDone = new ConcurrentQueue<WorkDone>();

            foreach (var queueName in workerQueueNames)
            {
                StartWorkerBus(queueName, workDone);

                service.AddDestinationQueue(queueName);

                queuesToReset.Add(queueName);
            }

            service.Start();

            var sender = Configure.With(TrackDisposable(new BuiltinContainerAdapter()))
                .MessageOwnership(o => o.Use(this))
                .Transport(t => t.UseMsmqInOneWayClientMode())
                .CreateBus().Start();

            var messagesToSend = Enumerable.Range(0, numberOfMessages)
                .Select(id => new Work {MessageId = id})
                .ToList();

            messagesToSend.ForEach(sender.Send);

            var waitStartTime = DateTime.UtcNow;

            while (waitStartTime.ElapsedUntilNow() < TimeSpan.FromSeconds(5 + (numberOfMessages/100)))
            {
                Thread.Sleep(100);

                if (workDone.Count >= numberOfMessages) break;
            }

            Thread.Sleep(2.Seconds());

            workDone.Count.ShouldBe(numberOfMessages);
            workDone.Select(w => w.MessageId).OrderBy(w => w)
                .ShouldBe(Enumerable.Range(0, numberOfMessages));

            var groupedByWorkers = workDone.GroupBy(w => w.WorkerQueueName);

            Console.WriteLine(@"Messages were processed like this:

{0}", string.Join(Environment.NewLine, groupedByWorkers.Select(g => string.Format("    " + g.Key + ": " + new string('*', g.Count())))));

            groupedByWorkers.Count().ShouldBe(numberOfWorkerEndpoints);
        }

        class WorkDone
        {
            public string WorkerQueueName { get; set; }
            public int MessageId { get; set; }
        }

        class Work
        {
            public int MessageId { get; set; }
        }

        void StartWorkerBus(string queueName, ConcurrentQueue<WorkDone> workDone)
        {
            var adapter = new BuiltinContainerAdapter();

            adapter.Handle<Work>(w => workDone.Enqueue(new WorkDone
            {
                MessageId = w.MessageId,
                WorkerQueueName = queueName
            }));

            Configure.With(TrackDisposable(adapter))
                .Logging(l => l.ColoredConsole(minLevel:LogLevel.Warn))
                .Transport(t => t.UseMsmq(queueName, "error"))
                .CreateBus()
                .Start();
        }

        public string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof (Work))
            {
                return LoadBalancerInputQueueName;
            }

            throw new ArgumentException(string.Format("Don't know where to send {0}!!", messageType));
        }
    }
}