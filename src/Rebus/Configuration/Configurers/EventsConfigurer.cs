using System.Linq;

namespace Rebus.Configuration.Configurers
{
    public class EventsConfigurer : IRebusEvents
    {
        public event MessageSentEventHandler MessageSent;
        public event MessageReceivedEventHandler MessageReceived;
        public event BeforeTransportMessageEventHandler BeforeTransportMessage;
        public event AfterTransportMessageEventHandler AfterTransportMessage;
        public event PoisonMessageEventHandler PoisonMessage;

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

            if (MessageReceived != null)
            {
                foreach (var listener in MessageReceived.GetInvocationList().Cast<MessageReceivedEventHandler>())
                {
                    rebusEvents.MessageReceived += listener;
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
        }
    }
}