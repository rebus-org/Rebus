using System;
using System.Threading;
using System.Transactions;
using Rebus.Logging;
using Rebus.Shared;
using Rebus.Transports.Msmq;

namespace Rebus.Gateway.Outbound
{
    public class OutboundService
    {
        static ILog log;

        static OutboundService()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly string listenQueue;
        readonly string destinationUri;
        readonly string errorQueue;

        volatile bool keepRunning = true;
        Thread workerThread;

        public OutboundService(string listenQueue, string destinationUri, string errorQueue)
        {
            this.listenQueue = listenQueue;
            this.destinationUri = destinationUri;
            this.errorQueue = errorQueue;
        }

        public void Start()
        {
            log.Info("Starting");

            workerThread = new Thread(StartWorker);
            workerThread.Start();
        }

        void StartWorker()
        {
            using (var messageQueue = new MsmqMessageQueue(listenQueue))
            {
                while (keepRunning)
                {
                    DoWork(messageQueue);
                }
            }
        }

        void DoWork(MsmqMessageQueue messageQueue)
        {
            try
            {
                using (var tx = new TransactionScope())
                {
                    var receivedTransportMessage = messageQueue.ReceiveMessage();

                    if (receivedTransportMessage != null)
                    {
                        try
                        {
                            TryToSendMessage(receivedTransportMessage);

                            tx.Complete();
                        }
                        catch (Exception e)
                        {
                            log.Error(e, "Could not send message {0} to destination URI {1} - waiting 1 sec before retrying",
                                      receivedTransportMessage.Id, destinationUri);

                            Thread.Sleep(TimeSpan.FromSeconds(1));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Unhandled exception during receive operation", e);
            }
        }

        void TryToSendMessage(ReceivedTransportMessage receivedTransportMessage)
        {
            log.Info("Trying to send message {0} to {1}", receivedTransportMessage.Id, destinationUri);

            throw new NotImplementedException("whoa!!! not implemented!1");
        }

        public void Stop()
        {
            log.Info("Stopping");

            keepRunning = false;
            workerThread.Join();
        }
    }
}