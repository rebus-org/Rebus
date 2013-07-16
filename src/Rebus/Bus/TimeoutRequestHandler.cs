using System;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Timeout;

namespace Rebus.Bus
{
    class TimeoutRequestHandler : IHandleMessages<TimeoutRequest>
    {
        static ILog log;

        static TimeoutRequestHandler()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly IStoreTimeouts storeTimeouts;

        public TimeoutRequestHandler(IStoreTimeouts storeTimeouts)
        {
            this.storeTimeouts = storeTimeouts;
        }

        public void Handle(TimeoutRequest message)
        {
            var currentMessageContext = MessageContext.GetCurrent();
            
            if (string.IsNullOrWhiteSpace(currentMessageContext.ReturnAddress))
            {
                throw new InvalidOperationException("TimeoutRequest received with no ReturnAddress header set. No way to return message when timeout elapses.");
            }

            var newTimeout = new Timeout.Timeout(currentMessageContext.ReturnAddress,
                                                 message.CorrelationId,
                                                 RebusTimeMachine.Now() + message.Timeout,
                                                 message.SagaId,
                                                 message.CustomData);

            storeTimeouts.Add(newTimeout);

            log.Info("Added new timeout: {0}", newTimeout);
        }
    }
}