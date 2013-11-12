using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Timer = System.Timers.Timer;

namespace Rebus.Transports.Showdown.Core
{
    public class ShowdownRunner : IDisposable, IDetermineMessageOwnership
    {
        const int MessageCount = 100000;
        const int NumberOfWorkers = 15;

        readonly string testShowdownReceiverInputQueue;
        readonly BuiltinContainerAdapter senderAdapter = new BuiltinContainerAdapter();
        readonly BuiltinContainerAdapter receiverAdapter = new BuiltinContainerAdapter();
        readonly string resultsFileName;

        public ShowdownRunner(string testShowdownReceiverInputQueue)
        {
            // default to no logging
            RebusLoggerFactory.Current = new NullLoggerFactory();
            this.testShowdownReceiverInputQueue = testShowdownReceiverInputQueue;
            resultsFileName = GenerateFileName();
            EnsureDirectoryExists(resultsFileName);
        }

        void EnsureDirectoryExists(string fileName)
        {
            var dir = Path.GetDirectoryName(fileName);
            if (Directory.Exists(dir)) return;
            Directory.CreateDirectory(dir);
        }

        string GenerateFileName()
        {
            string potentialFileName;
            var counter = 1;
            do
            {
                potentialFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "results",
                                                 string.Format("output-{0}.txt", counter++));
            } while (File.Exists(potentialFileName));

            return potentialFileName;
        }

        public BuiltinContainerAdapter SenderAdapter
        {
            get { return senderAdapter; }
        }

        public BuiltinContainerAdapter ReceiverAdapter
        {
            get { return receiverAdapter; }
        }

        public void Run()
        {
            try
            {
                using (var printTimer = new Timer())
                {
                    var sentMessagesCount = 0;
                    var receivedMessagesCount = 0;
                    printTimer.Interval = 10000;
                    printTimer.Elapsed +=
                        delegate
                        {
                            Print("Sent {0} messages. Received {1} messages.", sentMessagesCount, receivedMessagesCount);
                        };
                    printTimer.Start();

                    Print(@"----------------------------------------------------------------------
Running showdown: {0}
----------------------------------------------------------------------",
                        Assembly.GetCallingAssembly()
                            .GetName()
                            .Name);

                    var receivedMessageIds = new ConcurrentDictionary<int, int>();
                    var receivedMessages = 0;

                    Print("Stopping all workers in receiver");
                    var receiverBus = (RebusBus) receiverAdapter.Bus;
                    receiverBus.SetNumberOfWorkers(0);

                    Thread.Sleep(TimeSpan.FromSeconds(2));

                    Print("Sending {0} messages from sender to receiver", MessageCount);

                    var senderWatch = Stopwatch.StartNew();
                    Enumerable.Range(1, MessageCount)
                        .Select(i => new TestMessage {MessageId = i})
                        .ToList()
                        .ForEach(message =>
                                 {
                                     receivedMessageIds[message.MessageId] = 0;
                                     senderAdapter.Bus.Send(message);
                                     Interlocked.Increment(ref sentMessagesCount);
                                 });

                    var totalSecondsSending = senderWatch.Elapsed.TotalSeconds;
                    Print("Sending {0} messages took {1:0.0} s ({2:0.0} msg/s)",
                        MessageCount, totalSecondsSending, MessageCount/totalSecondsSending);

                    var resetEvent = new ManualResetEvent(false);

                    receiverAdapter
                        .Handle<TestMessage>(message =>
                                             {
                                                 var result = Interlocked.Increment(ref receivedMessages);

                                                 if (result == MessageCount)
                                                 {
                                                     resetEvent.Set();
                                                 }

                                                 Interlocked.Increment(ref receivedMessagesCount);
                                             });

                    Print("Starting receiver with {0} workers", NumberOfWorkers);

                    var receiverWatch = Stopwatch.StartNew();
                    receiverBus.SetNumberOfWorkers(NumberOfWorkers);

                    resetEvent.WaitOne();
                    var totalSecondsReceiving = receiverWatch.Elapsed.TotalSeconds;

                    Thread.Sleep(2000);

                    Print("Receiving {0} messages took {1:0.0} s ({2:0.0} msg/s)",
                        MessageCount, totalSecondsReceiving, MessageCount/totalSecondsReceiving);
                }
            }
            catch (Exception e)
            {
                Print("Error: {0}", e);
            }
        }

        void Print(string message, params object[] objs)
        {
            Console.WriteLine(message, objs);
            File.AppendAllText(resultsFileName, string.Format(message, objs) + Environment.NewLine);
        }

        public void Dispose()
        {
            if (disposing || disposed) return;

            lock (this)
            {
                if (disposing || disposed) return;
                
                try
                {
                    disposing = true;
                    senderAdapter.Dispose();
                    receiverAdapter.Dispose();
                }
                finally
                {
                    disposed = true;
                    disposing = false;
                }
            }
        }

        bool disposed;
        bool disposing;

        public string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof (TestMessage))
                return testShowdownReceiverInputQueue;

            throw new ArgumentException(string.Format("Don't have an endpoint mapping for {0}", messageType));
        }

        class TestMessage
        {
            public int MessageId { get; set; }
        }
    }
}
