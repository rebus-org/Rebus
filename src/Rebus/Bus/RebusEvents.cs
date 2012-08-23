using System;

namespace Rebus.Bus
{
    class RebusEvents : IRebusEvents
    {
        public event MessageSentEventHandler MessageSent = delegate { };

        public event BeforeMessageEventHandler BeforeMessage = delegate { };

        public event AfterMessageEventHandler AfterMessage = delegate { };

        public event UncorrelatedMessageEventHandler UncorrelatedMessage = delegate { };

        public event BeforeTransportMessageEventHandler BeforeTransportMessage = delegate { };

        public event AfterTransportMessageEventHandler AfterTransportMessage = delegate { };

        public event PoisonMessageEventHandler PoisonMessage = delegate { };

        internal void RaiseMessageSent(IAdvancedBus advancedBus, string destination, object message)
        {
            MessageSent(advancedBus, destination, message);
        }

        internal void RaiseBeforeMessage(IAdvancedBus advancedBus, object message)
        {
            BeforeMessage(advancedBus, message);
        }

        internal void RaiseAfterMessage(IAdvancedBus bus, Exception exception, object message)
        {
            AfterMessage(bus, exception, message);
        }

        internal void RaiseBeforeTransportMessage(IAdvancedBus advancedBus, ReceivedTransportMessage transportMessage)
        {
            BeforeTransportMessage(advancedBus, transportMessage);
        }

        internal void RaiseAfterTransportMessage(IAdvancedBus advancedBus, Exception exception, ReceivedTransportMessage transportMessage)
        {
            AfterTransportMessage(advancedBus, exception, transportMessage);
        }

        internal void RaisePoisonMessage(IAdvancedBus advancedBus, ReceivedTransportMessage transportMessage)
        {
            PoisonMessage(advancedBus, transportMessage);
        }

        internal void RaiseUncorrelatedMessage(object message, Saga saga)
        {
            UncorrelatedMessage(message, saga);
        }
    }
}