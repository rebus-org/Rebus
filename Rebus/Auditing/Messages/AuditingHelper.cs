using System;
using Rebus.Messages;
using Rebus.Time;
using Rebus.Transport;

namespace Rebus.Auditing.Messages;

sealed class AuditingHelper
{
    readonly ITransport _transport;
    readonly IRebusTime _rebusTime;
        
    bool _didInitializeAuditQueue;

    public AuditingHelper(ITransport transport, string auditQueueName, IRebusTime rebusTime)
    {
        AuditQueueName = auditQueueName ?? throw new ArgumentNullException(nameof(auditQueueName));
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
    }

    public string AuditQueueName { get; }

    public void EnsureAuditQueueHasBeenCreated()
    {
        if (_didInitializeAuditQueue) return;

        _transport.CreateQueue(AuditQueueName);

        _didInitializeAuditQueue = true;
    }

    public void SetCommonHeaders(TransportMessage transportMessage)
    {
        if (transportMessage == null) throw new ArgumentNullException(nameof(transportMessage));

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