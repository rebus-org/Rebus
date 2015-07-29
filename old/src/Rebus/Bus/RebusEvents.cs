using System;
using System.Collections.Generic;

using Rebus.Messages;

namespace Rebus.Bus
{
    class RebusEvents : IRebusEvents
    {
        readonly List<IUnitOfWorkManager> unitOfWorkManagers = new List<IUnitOfWorkManager>();

        public RebusEvents()
        {
            MessageMutators = new List<IMutateMessages>();
        }

        public event MessageSentEventHandler MessageSent = delegate { };

        public event BeforeInternalSendEventHandler BeforeInternalSend = delegate { };

        public event BeforeMessageEventHandler BeforeMessage = delegate { };

        public event AfterMessageEventHandler AfterMessage = delegate { };

        public event UncorrelatedMessageEventHandler UncorrelatedMessage = delegate { };

        public event MessageContextEstablishedEventHandler MessageContextEstablished = delegate { };

        public event BusStartedEventHandler BusStarted = delegate { };
        
        public event BusStoppedEventHandler BusStopped = delegate { };
        
        public event BeforeTransportMessageEventHandler BeforeTransportMessage = delegate { };

        public event AfterTransportMessageEventHandler AfterTransportMessage = delegate { };
        
        public event MessageAuditedEventHandler MessageAudited = delegate { };

        public event PoisonMessageEventHandler PoisonMessage = delegate { };

        public event OnHandlingErrorEventHandler OnHandlingError = delegate { };

        public event AfterHandlingEventHandler AfterHandling = delegate { };

        public event BeforeHandlingEventHandler BeforeHandling = delegate { };

        public ICollection<IMutateMessages> MessageMutators { get; private set; }

        public void AddUnitOfWorkManager(IUnitOfWorkManager unitOfWorkManager)
        {
            unitOfWorkManagers.Add(unitOfWorkManager);
        }

        internal IEnumerable<IUnitOfWorkManager> UnitOfWorkManagers
        {
            get { return unitOfWorkManagers; }
        }

        internal void RaiseMessageContextEstablished(IBus bus, IMessageContext messageContext)
        {
            MessageContextEstablished(bus, messageContext);
        }

        internal void RaiseMessageSent(IBus bus, string destination, object message)
        {
            MessageSent(bus, destination, message);
        }

        internal void RaiseBeforeInternalSend(IEnumerable<string> destinations, Message message, bool published)
        {
            BeforeInternalSend(destinations, message, published);
        }

        internal void RaiseBeforeMessage(IBus bus, object message)
        {
            BeforeMessage(bus, message);
        }

        internal void RaiseAfterMessage(IBus bus, Exception exception, object message)
        {
            AfterMessage(bus, exception, message);
        }

        internal void RaiseBusStarted(IBus bus)
        {
            BusStarted(bus);
        }

        internal void RaiseBusStopped(IBus bus)
        {
            BusStopped(bus);
        }

        internal void RaiseBeforeTransportMessage(IBus bus, ReceivedTransportMessage transportMessage)
        {
            BeforeTransportMessage(bus, transportMessage);
        }

        internal void RaiseAfterTransportMessage(IBus bus, Exception exception, ReceivedTransportMessage transportMessage)
        {
            AfterTransportMessage(bus, exception, transportMessage);
        }

        internal void RaisePoisonMessage(IBus bus, ReceivedTransportMessage transportMessage, PoisonMessageInfo poisonMessageInfo)
        {
            PoisonMessage(bus, transportMessage, poisonMessageInfo);
        }

        internal void RaiseUncorrelatedMessage(IBus bus, object message, Saga saga)
        {
            UncorrelatedMessage(bus, message, saga);
        }

        internal void RaiseMessageAudited(IBus bus, TransportMessageToSend transportMessage)
        {
            MessageAudited(bus, transportMessage);
        }

        internal void RaiseOnHandlingError(Exception exception)
        {
            OnHandlingError(exception);
        }

        internal void RaiseAfterHandling(IBus bus, object message, IHandleMessages handler)
        {
            AfterHandling(bus, message, handler);
        }

        internal void RaiseBeforeHandling(IBus bus, object message, IHandleMessages handler)
        {
            BeforeHandling(bus, message, handler);
        }
    }
}