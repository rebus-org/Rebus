using System;
using Rebus.Gateway.Inbound;
using Rebus.Gateway.Outbound;
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

        InboundService inboundService;
        OutboundService outboundService;

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
            log.Info("Starting outbound service...");
            outboundService = new OutboundService(ListenQueue, DestinationUri);
            outboundService.Start();
        }

        void InitHttpListener()
        {
            log.Info("Starting inbound service...");
            inboundService = new InboundService(ListenUri, DestinationQueue);
            inboundService.Start();
        }

        public void Stop()
        {
            if (inboundService != null)
            {
                log.Info("Stopping inbound service...");
                inboundService.Stop();
            }

            if (outboundService != null)
            {
                log.Info("Stopping outbound service...");
                outboundService.Stop();
            }
        }
    }
}