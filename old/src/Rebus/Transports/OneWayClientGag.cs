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
        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public ReceivedTransportMessage ReceiveMessage(ITransactionContext context)
        {
            throw GagException();
        }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public string InputQueue { get { throw GagException(); } }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public string InputQueueAddress { get { throw GagException(); } }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public void StopTracking(string id)
        {
            throw GagException();
        }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public bool MessageHasFailedMaximumNumberOfTimes(string id)
        {
            throw GagException();
        }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public string GetErrorText(string id)
        {
            throw GagException();
        }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public PoisonMessageInfo GetPoisonMessageInfo(string id)
        {
            throw GagException();
        }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public void SetMaxRetriesFor<TException>(int maxRetriesForThisExceptionType) where TException : Exception
        {
            throw GagException();
        }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public void TrackDeliveryFail(string id, Exception exception)
        {
            throw GagException();
        }

        /// <summary>
        /// The <see cref="OneWayClientGag"/> must not accidentally be used, so this operation will throw a <see cref="InvalidOperationException"/>
        /// </summary>
        public string ErrorQueueAddress { get { throw GagException(); } }

        static InvalidOperationException GagException()
        {
            return
                new InvalidOperationException(
                    "The bus' ability to perform all kinds of message recipient-related activities has been" +
                    " gagged by the OneWayClientGag. This is most likely because the bus is configured in" +
                    " some kind of one-way client mode, which makes operations like ReceiveMessage" +
                    " nonsensical.");
        }
    }
}