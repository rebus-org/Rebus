using System;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Timeout;

namespace Rebus.Bus
{
    class TimeoutRequestHandler : IHandleMessages<TimeoutRequest>, IDisposable
    {
        static ILog log;

        static TimeoutRequestHandler()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        readonly IStoreTimeouts storeTimeouts;
        readonly DueTimeoutScheduler scheduler;

        public TimeoutRequestHandler(IStoreTimeouts storeTimeouts, IHandleDeferredMessage handleDeferredMessage)
        {
            this.storeTimeouts = storeTimeouts;
            scheduler = new DueTimeoutScheduler(storeTimeouts, handleDeferredMessage);
        }

        public void Handle(TimeoutRequest message)
        {
            var currentMessageContext = MessageContext.GetCurrent();

            var newTimeout = new Timeout.Timeout(currentMessageContext.ReturnAddress,
                                                 message.CorrelationId,
                                                 RebusTimeMachine.Now() + message.Timeout,
                                                 message.SagaId,
                                                 message.CustomData);

            storeTimeouts.Add(newTimeout);

            log.Info("Added new timeout: {0}", newTimeout);
        }

        public void Dispose()
        {
            scheduler.Dispose();
        }
    }
}