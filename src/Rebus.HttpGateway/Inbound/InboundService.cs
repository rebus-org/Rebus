using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Rebus.Logging;
using System.Linq;
using Rebus.Transports.Msmq;

namespace Rebus.HttpGateway.Inbound
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
        static readonly Encoding Encoding = Encoding.UTF8;

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
                if (!asyncResult.IsCompleted) return;
                if (!httpListener.IsListening) return;

                var context = httpListener.EndGetContext(asyncResult);

                var request = context.Request;
                var response = context.Response;

                try
                {
                    switch (request.HttpMethod.ToLowerInvariant())
                    {
                        case "post":
                            HandlePost(response, request);
                            break;

                        default:
                            response.StatusCode = (int) HttpStatusCode.MethodNotAllowed;
                            response.Close();
                            break;
                    }
                }
                catch(Exception e)
                {
                    HandleServerError(response, e);
                }


                httpListener.BeginGetContext(HandleIncomingHttpRequest, null);
            }
            catch (Exception e)
            {
                log.Warn("Unhandled exception while receiving request: {0} - shutting down application", e);
            }
        }

        static void HandleServerError(HttpListenerResponse response, Exception e)
        {
            log.Error(e, "An error occurred while handling HTTP request");

            response.StatusCode = (int) HttpStatusCode.InternalServerError;
            response.ContentEncoding = Encoding;
            response.ContentType = Encoding.WebName;

            var bytes = Encoding.GetBytes(e.ToString());
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);

            response.Close();
        }

        void HandlePost(HttpListenerResponse response, HttpListenerRequest request)
        {
            using (var reader = new BinaryReader(request.InputStream))
            {
                var receivedTransportMessage = new ReceivedTransportMessage
                    {
                        Id = request.Headers[RebusHttpHeaders.Id],
                        Body = reader.ReadBytes((int) request.ContentLength64)
                    };

                var headers = new Dictionary<string, string>();

                foreach (var rebusHeaderKey in request.Headers.AllKeys.Where(k => k.StartsWith(RebusHttpHeaders.CustomHeaderPrefix)))
                {
                    var value = request.Headers[rebusHeaderKey];
                    var key = rebusHeaderKey.Substring(RebusHttpHeaders.CustomHeaderPrefix.Length);

                    headers.Add(key, value);
                }

                log.Info("Got headers in request: {0}", string.Join(", ", headers.Keys));

                receivedTransportMessage.Headers = headers;

                log.Info("Received message {0}", receivedTransportMessage.Id);

                using (var queue = MsmqMessageQueue.Sender())
                {
                    queue.Send(destinationQueue, receivedTransportMessage.ToForwardableMessage());
                }

                log.Info("Message was sent to {0}", destinationQueue);

                response.StatusCode = (int) HttpStatusCode.OK;
                response.Close();
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
            httpListener.Close();
        }
    }
}