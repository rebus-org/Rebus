using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Transactions;
using Newtonsoft.Json;
using Rebus.Logging;
using Rebus.Serialization.Json;
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

        volatile bool keepRunning = true;
        Thread workerThread;
        static readonly Encoding Encoding = Encoding.UTF8;

        public OutboundService(string listenQueue, string destinationUri)
        {
            this.listenQueue = listenQueue;
            this.destinationUri = destinationUri;
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

        void DoWork(IReceiveMessages messageQueue)
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
            request.GetRequestStream().Write(bytes, 0, bytes.Length);

            foreach(var header in receivedTransportMessage.Headers)
            {
                request.Headers.Add("x-rebus-custom-" + header.Key, header.Value);
            }

            request.Headers.Add("x-rebus-message-ID", receivedTransportMessage.Id);

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                log.Info("Message {0} was sent", receivedTransportMessage.Id);

                reader.ReadToEnd();
            }
        }

        public void Stop()
        {
            log.Info("Stopping");

            keepRunning = false;
            workerThread.Join();
        }
    }
}