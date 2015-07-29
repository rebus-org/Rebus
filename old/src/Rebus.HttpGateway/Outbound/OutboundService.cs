using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Transactions;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Transports.Msmq;
using System.Linq;

namespace Rebus.HttpGateway.Outbound
{
    public class OutboundService
    {
        static ILog log;

        static OutboundService()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        static readonly Encoding Encoding = Encoding.UTF8;

        volatile bool keepRunning = true;
        readonly string listenQueue;
        readonly string destinationUri;
        Thread workerThread;

        public OutboundService(string listenQueue, string destinationUri)
        {
            this.listenQueue = listenQueue;
            this.destinationUri = destinationUri;
        }

        public void Start()
        {
            if (workerThread != null)
            {
                throw new InvalidOperationException("Apparently, Start() was called twice. " + ErrorText.GenericStartStopErrorHelpText);
            }

            log.Info("Starting");

            workerThread = new Thread(StartWorker);
            workerThread.Start();
        }

        public void Stop()
        {
            if (workerThread == null)
            {
                throw new InvalidOperationException("Apparently, Stop() was called without any calls to Start(). " + ErrorText.GenericStartStopErrorHelpText);
            }

            log.Info("Stopping");

            keepRunning = false;
            workerThread.Join();
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

        void DoWork(IReceiveMessages messageQueue)
        {
            try
            {
                using (var tx = new TransactionScope())
                {
                    var ctx = new AmbientTransactionContext();
                    var receivedTransportMessage = messageQueue.ReceiveMessage(ctx);

                    if (receivedTransportMessage == null) return;

                    try
                    {
                        SendMessage(receivedTransportMessage);

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
            catch (Exception e)
            {
                log.Error("Unhandled exception during receive operation", e);
            }
        }

        void SendMessage(ReceivedTransportMessage receivedTransportMessage)
        {
            log.Info("Trying to send message {0} to {1}", receivedTransportMessage.Id, destinationUri);

            var request = (HttpWebRequest)WebRequest.Create(destinationUri);
            request.Method = "POST";

            request.ContentType = Encoding.WebName;

            var bytes = receivedTransportMessage.Body;
            request.ContentLength = bytes.Length;
            
            var headers = receivedTransportMessage.Headers.ToDictionary(d => d.Key, d => d.Value);

            foreach (var header in headers)
            {
                request.Headers.Add(RebusHttpHeaders.CustomHeaderPrefix + header.Key, (string)header.Value);
            }

            request.Headers.Add(RebusHttpHeaders.Id, receivedTransportMessage.Id);

            using(var requestStream = request.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
            }

            log.Info("Added headers to request: {0}", string.Join(", ", headers.Keys));

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                log.Info("Message {0} was sent", receivedTransportMessage.Id);

                reader.ReadToEnd();
            }
        }
    }
}