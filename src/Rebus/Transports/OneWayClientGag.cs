using System;
using Rebus.Bus;

namespace Rebus.Transports
{
    /// <summary>
    /// Dummy implementation of <see cref="IReceiveMessages"/> that gags the service, causing it
    /// to experience all kinds of exceptions if it attempts to receive a message.
    /// </summary>
    public class OneWayClientGag : IReceiveMessages, IErrorTracker
    {
        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            throw GagException();
        }

        public string InputQueue { get; private set; }

        public string InputQueueAddress { get; private set; }
        
        public void StopTracking(string id)
        {
            throw GagException();
        }

        public bool MessageHasFailedMaximumNumberOfTimes(string id)
        {
            throw GagException();
        }

        public string GetErrorText(string id)
        {
            throw GagException();
        }

        public PoisonMessageInfo GetPoisonMessageInfo(string id)
        {
            throw GagException();
        }

        public void TrackDeliveryFail(string id, Exception exception)
        {
            throw GagException();
        }

        public string ErrorQueueAddress { get; private set; }

        static NotImplementedException GagException()
        {
            return new NotImplementedException("The bus' ability to perform all kinds of message recipient-related activities has been" +
                                               " gagged by the OneWayClientGag. This is most likely because the bus is configured in" +
                                               " some kind of one-way client mode, which makes operations like ReceiveMessage" +
                                               " nonsensical.");
        }
    }
}