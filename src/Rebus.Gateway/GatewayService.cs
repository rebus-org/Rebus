using System;
using System.Configuration;
using System.IO;
using System.Net;
using Rebus.Logging;

namespace Rebus.Gateway
{
    public class GatewayService
    {
        static ILog log;

        static GatewayService()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        HttpListener httpListener;

        public string ListenQueue { get; set; }
        public string DestinationUri { get; set; }

        public string DestinationQueue { get; set; }
        public string ListenUri { get; set; }

        public void Start()
        {
            if (string.IsNullOrEmpty(ListenUri) && string.IsNullOrEmpty(ListenQueue))
            {
                throw new InvalidOperationException(string.Format(@"
Cannot start the gateway, since ListenUri and ListenQueue are both empty!

You need to equip the gateway with enough information to at least work in
one-way mode. Available modes are described here:

{0}", GenericHelpText()));
            }

            if (string.IsNullOrEmpty(ListenUri))
            {
                log.Info("No listen URI has been configured - gateway service is running in one-way mode...");
            }
            else
            {
                InitHttpListener();
            }

            if (string.IsNullOrEmpty(ListenQueue))
            {
                log.Info("No listen queue name has been configured - gateway service is running in one-way mode...");
            }
            else
            {
                InitQueueListener();
            }

            log.Info("Started!");
        }

        string GenericHelpText()
        {
            return @"
The gateway can work in one of three modes: inbound, outbound, or full duplex.

    Inbound:
        In this mode, the gateway has an HTTP endpoint that listens to incoming
        messages, which are then put in a queue. In this mode, a ListenUri and
        a DestinationQueue must be configured.

    Outbound:
        In this mode, the gateway receives messages out of a queue, which are
        then sent to another gateway with an HTTP request. In this mode, a
        ListenQueue and a DestinationUri must be configured.

    Full Duplex:
        In this mode, the gateway works in a combined inbound/outbound mode,
        and thus all the parameters of both inbound and outbound modes must
        be configured.
";
        }

        void InitQueueListener()
        {
            log.Info("Starting MSMQ listener...");
            
        }

        void InitHttpListener()
        {
            log.Info("Starting HTTP listener...");

            httpListener = new HttpListener();
            httpListener.Prefixes.Add(ListenUri);
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

                using(var reader = new StreamReader(request.InputStream))
                {
                    var readToEnd = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                log.Warn("Error while receiving request: {0}", e);
            }
        }

        public void Stop()
        {
            log.Info("Stopping HTTP listener...");
            httpListener.Stop();
            log.Info("Stopped!");
        }
    }
}