using System;
using System.IO;
using System.Net;
using Rebus.Logging;
using Rebus.Transports.Msmq;

namespace Rebus.Gateway.Inbound
{
    public class InboundService
    {
        readonly string listenUri;
        readonly string destinationQueue;
        static ILog log;

        static InboundService()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        HttpListener httpListener;

        public InboundService(string listenUri, string destinationQueue)
        {
            this.listenUri = listenUri;
            this.destinationQueue = destinationQueue;
        }

        public void Start()
        {
            log.Info("Starting");
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(GetListenUri());
            httpListener.Start();
            httpListener.BeginGetContext(HandleIncomingHttpRequest, null);

            
        }

        void HandleIncomingHttpRequest(IAsyncResult asyncResult)
        {
            try
            {
                var context = httpListener.EndGetContext(asyncResult);

                var request = context.Request;
                log.Debug("Got request from {0}", request.UserHostAddress);

                using (var reader = new StreamReader(request.InputStream))
                {
                    var readToEnd = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                log.Warn("Error while receiving request: {0}", e);
            }
        }

        string GetListenUri()
        {
            if (listenUri.EndsWith("/"))
                return listenUri;

            return listenUri + "/";
        }

        public void Stop()
        {
            log.Info("Stopping");
            httpListener.Stop();
        }
    }
}