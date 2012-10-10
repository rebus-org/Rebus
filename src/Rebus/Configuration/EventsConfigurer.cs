using System.Collections.Generic;
using System.Linq;

namespace Rebus.Configuration
{
    public class EventsConfigurer : BaseConfigurer, IRebusEvents
    {
        public EventsConfigurer(ConfigurationBackbone backbone)
            : base(backbone)
        {
            backbone.AddEvents(this);

            MessageMutators = new List<IMutateMessages>();
        }

        public event MessageSentEventHandler MessageSent;
        public event BeforeMessageEventHandler BeforeMessage;
        public event AfterMessageEventHandler AfterMessage;
        public event UncorrelatedMessageEventHandler UncorrelatedMessage;
        public event BeforeTransportMessageEventHandler BeforeTransportMessage;
        public event AfterTransportMessageEventHandler AfterTransportMessage;
        public event PoisonMessageEventHandler PoisonMessage;

        public ICollection<IMutateMessages> MessageMutators { get; private set; }

        public void TransferToBus(IAdvancedBus advancedBus)
        {
            var rebusEvents = advancedBus.Events;

            if (MessageSent != null)
            {
                foreach (var listener in MessageSent.GetInvocationList().Cast<MessageSentEventHandler>())
                {
                    rebusEvents.MessageSent += listener;
                }
            }

            if (BeforeMessage != null)
            {
                foreach (var listener in BeforeMessage.GetInvocationList().Cast<BeforeMessageEventHandler>())
                {
                    rebusEvents.BeforeMessage += listener;
                }
            }

            if (AfterMessage != null)
            {
                foreach (var listener in AfterMessage.GetInvocationList().Cast<AfterMessageEventHandler>())
                {
                    rebusEvents.AfterMessage += listener;
                }
            }

            if (BeforeTransportMessage != null)
            {
                foreach (var listener in BeforeTransportMessage.GetInvocationList().Cast<BeforeTransportMessageEventHandler>())
                {
                    rebusEvents.BeforeTransportMessage += listener;
                }
            }

            if (AfterTransportMessage != null)
            {
                foreach (var listener in AfterTransportMessage.GetInvocationList().Cast<AfterTransportMessageEventHandler>())
                {
                    rebusEvents.AfterTransportMessage += listener;
                }
            }

            if (PoisonMessage != null)
            {
                foreach (var listener in PoisonMessage.GetInvocationList().Cast<PoisonMessageEventHandler>())
                {
                    rebusEvents.PoisonMessage += listener;
                }
            }

            if (UncorrelatedMessage != null)
            {
                foreach (var listener in UncorrelatedMessage.GetInvocationList().Cast<UncorrelatedMessageEventHandler>())
                {
                    rebusEvents.UncorrelatedMessage += listener;
                }
            }

            foreach(var messageMutator in MessageMutators)
            {
                rebusEvents.MessageMutators.Add(messageMutator);
            }
        }
    }
}