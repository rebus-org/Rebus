using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Rebus.Bus;
using Rebus.Logging;
using System.Linq;
using Rebus.Transports.Msmq;

namespace Rebus.HttpGateway.Inbound
{
    public class InboundService
    {
        const int AccessDeniedErrorCode = 5;
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

        public string OutboundListenerQueue { get; set; }

        public void Start()
        {
            if (httpListener != null)
            {
                throw new InvalidOperationException("Apparently, Start() was called twice. " + ErrorText.GenericStartStopErrorHelpText);
            }
            
            log.Info("Starting");

            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add(GetListenUri());
                httpListener.Start();
                httpListener.BeginGetContext(HandleIncomingHttpRequest, null);
            }
            catch(HttpListenerException ex)
            {
                if (ex.ErrorCode == AccessDeniedErrorCode)
                {
                    throw new InvalidOperationException(string.Format(@"The HTTP gateway failed to initialize the incoming HTTP listener. 

The listener requires specific rights to allow it to start. You can fix this in one of two ways:

1. Run the following command from a command prompt:
netsh http add urlacl url={0} user=""NTAuthority\Authenticated Users"" sddl=""D:(A;;GX;;;AU)""

2. Run the HTTP gateway under an administrative account.

The first solution is recommended for servers that require production level security. Consider restricting the user part 
of the netsh command further. For more information about the SDDL string at the end, check: 

http://www.netid.washington.edu/documentation/domains/sddl.aspx", GetListenUri()));
                }
                
                throw;
            }
        }

        public void Stop()
        {
            if (httpListener == null)
            {
                throw new InvalidOperationException("Apparently, Stop() was called without any calls to Start(). " + ErrorText.GenericStartStopErrorHelpText);
            }

            log.Info("Stopping");
            httpListener.Stop();
            httpListener.Close();
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

                var headers = new Dictionary<string, object>();

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
                    queue.Send(destinationQueue, receivedTransportMessage.ToForwardableMessage(), new NoTransaction());
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
    }
}