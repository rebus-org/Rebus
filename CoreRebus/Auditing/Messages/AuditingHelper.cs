using System;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Auditing.Messages
{
    class AuditingHelper
    {
        readonly ITransport _transport;
        bool _didInitializeAuditQueue;

        public AuditingHelper(ITransport transport, string auditQueue)
        {
            AuditQueue = auditQueue;
            _transport = transport;
        }

        public string AuditQueue { get; }

        public void EnsureAuditQueueHasBeenCreated()
        {
            if (_didInitializeAuditQueue) return;

            _transport.CreateQueue(AuditQueue);

            _didInitializeAuditQueue = true;
        }

        public void SetCommonHeaders(TransportMessage transportMessage)
        {
            var headers = transportMessage.Headers;

            if (_transport.Address != null)
            {
                headers[AuditHeaders.HandleQueue] = _transport.Address;
            }

            headers[AuditHeaders.AuditTime] = RebusTime.Now.ToString("O");
            headers[AuditHeaders.MachineName] = Environment.MachineName;
        }
    }
}