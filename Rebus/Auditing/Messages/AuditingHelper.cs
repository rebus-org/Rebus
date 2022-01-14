using System;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Auditing.Messages;

class AuditingHelper
{
    readonly ITransport _transport;
    readonly IRebusTime _rebusTime;
        
    bool _didInitializeAuditQueue;

    public AuditingHelper(ITransport transport, string auditQueue, IRebusTime rebusTime)
    {
        AuditQueue = auditQueue;
        _transport = transport;
        _rebusTime = rebusTime;
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

        headers[AuditHeaders.AuditTime] = _rebusTime.Now.ToString("O");
        headers[AuditHeaders.MachineName] = GetMachineName();
    }

    static string GetMachineName() => Environment.MachineName;
}