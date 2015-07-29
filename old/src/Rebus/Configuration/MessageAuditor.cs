using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Shared;

namespace Rebus.Configuration
{
    class MessageAuditor
    {
        static ILog log;
        static MessageAuditor()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public void Configure(IRebusEvents rebusEvents, string auditQueueName)
        {
            log.Info("Configuring Rebus to copy successfully processed messages to {0}", auditQueueName);

            rebusEvents.AfterTransportMessage +=
                (bus, exceptionOrNull, message) =>
                    PossiblyCopyToAuditQueue(auditQueueName, exceptionOrNull, bus, message);
        }

        static void PossiblyCopyToAuditQueue(string auditQueueName, Exception exceptionOrNull, IBus bus, ReceivedTransportMessage message)
        {
            // if an error occurred, don't do anything
            if (exceptionOrNull != null) return;

            // this one will always be non-null - but still
            if (TransactionContext.Current == null)
            {
                log.Warn("Auditor called outside of a proper transaction context!!! This must be an error.");
                return;
            }

            var rebusBus = bus as RebusBus;
            if (rebusBus == null)
            {
                log.Warn("Current IBus is not a RebusBus, it's a {0} - cannot use {0} for auditing, sorry!", bus.GetType().Name);
                return;
            }

            using (var txc = ManagedTransactionContext.Get())
            {
                var messageCopy = message.ToForwardableMessage();

                messageCopy.Headers[Headers.AuditReason] = Headers.AuditReasons.Handled;
                messageCopy.Headers[Headers.AuditSourceQueue] = rebusBus.GetInputQueueAddress();
                messageCopy.Headers[Headers.AuditMessageCopyTime] = RebusTimeMachine.Now().ToString("u");

                rebusBus.InternalSend(new List<string> {auditQueueName}, messageCopy, txc.Context);

                var rebusEvents = rebusBus.Events as RebusEvents;
                if (rebusEvents == null)
                {
                    log.Warn(
                        "Current IRebusEvents is not a RebusEvents, it's a {0} - cannot use {0} for raising auditing events, sorry! (the message was properly audited though, it just turned out to be impossible to raise the MessageAudited event!)");
                    return;
                }

                rebusEvents.RaiseMessageAudited(rebusBus, messageCopy);
            }
        }
    }
}