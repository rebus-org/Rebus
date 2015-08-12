using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Transports.Msmq;

namespace Rebus.MsmqLoadBalancer
{
    /// <summary>
    /// Special thin (MSMQ-only) Rebus endpoint that can function as a load balancer. Can be configured
    /// to receive messages from a queue which will then be forwarded to one of the available workers.
    /// Work is distributed in a round robin-style fashion.
    /// </summary>
    public class LoadBalancerService : IDisposable
    {
        static ILog log;

        readonly string inputQueueName;
        readonly int numberOfWorkers;

        int roundRobinCounter;
        static LoadBalancerService()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// Stores the destination queues of all the available workers.
        /// </summary>
        readonly List<string> destinationQueueNames = new List<string>();

        /// <summary>
        /// Stores all the currently active workers.
        /// </summary>
        readonly List<LoadBalancerWorker> workers = new List<LoadBalancerWorker>();
        
        MsmqMessageQueue queue;

        public LoadBalancerService(string inputQueueName, int numberOfWorkers = 1)
        {
            this.inputQueueName = inputQueueName;
            this.numberOfWorkers = numberOfWorkers;
        }

        public LoadBalancerService AddDestinationQueue(string destinationQueueName)
        {
            log.Info("Adding '{0}' as a worker destination", destinationQueueName);
            destinationQueueNames.Add(destinationQueueName);
            return this;
        }

        public LoadBalancerService Start()
        {
            if (!destinationQueueNames.Any())
            {
                throw new InvalidOperationException("Cannot start load balancer without adding at least one worker input queue!");
            }

            log.Info("Starting load balancer");

            queue = new MsmqMessageQueue(inputQueueName);

            workers.AddRange(Enumerable.Range(1, numberOfWorkers)
                .Select(i => new LoadBalancerWorker(queue, i, GetNextDestination)));

            return this;
        }

        string GetNextDestination()
        {
            var nextNumber = Interlocked.Increment(ref roundRobinCounter);
            var numberOfDestinations = destinationQueueNames.Count;
            
            // modulo gymnastics to ensure that the index is always positive
            var indexOfThisDestination = (nextNumber%numberOfDestinations + numberOfDestinations)
                                         %numberOfDestinations;

            return destinationQueueNames[indexOfThisDestination];
        }

        public void Dispose()
        {
            workers.ForEach(w => w.Stop());
            workers.ForEach(w => w.Dispose());
            workers.Clear();

            if (queue != null)
            {
                queue.Dispose();
            }
        }

        class LoadBalancerWorker : IDisposable
        {
            static ILog log;
            static LoadBalancerWorker()
            {
                RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
            }

            readonly MsmqMessageQueue queue;
            readonly int workerNumber;
            readonly Func<string> getNextDestination;
            readonly Thread workerThread;

            volatile bool keepWorking = true;

            public LoadBalancerWorker(MsmqMessageQueue queue, int workerNumber, Func<string> getNextDestination)
            {
                this.queue = queue;
                this.workerNumber = workerNumber;
                this.getNextDestination = getNextDestination;
                workerThread = new Thread(DoWork);

                log.Info("Starting load balancer worker {0}", workerNumber);
                workerThread.Start();
            }

            public void Stop()
            {
                if (!keepWorking) return;

                log.Info("Stopping load balancer worker {0}", workerNumber);
                keepWorking = false;
            }

            void DoWork()
            {
                while (keepWorking)
                {
                    try
                    {
                        using (var tx = new TransactionScope())
                        {
                            var transactionContext = new AmbientTransactionContext();
                            var message = queue.ReceiveMessage(transactionContext);
                            if (message == null)
                            {
                                Thread.Sleep(200);
                                continue;
                            }

                            var destinationForThisMessage = getNextDestination();

                            log.Debug("Received message {0} will be forwarded to {1}", message.Id, destinationForThisMessage);

                            queue.Send(destinationForThisMessage, message.ToForwardableMessage(), transactionContext);

                            tx.Complete();
                        }
                    }
                    catch (Exception exception)
                    {
                        log.Error("An error occurred while trying to process a message: {0}", exception);
                    }
                }
            }

            public void Dispose()
            {
                Stop();

                workerThread.Join(TimeSpan.FromSeconds(30));
            }
        }
    }
}
