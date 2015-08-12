using System;
using System.Collections.Generic;
using System.Linq;

namespace Rebus.Bus
{
    /// <summary>
    /// <see cref="RebusBus"/> will use its <see cref="IErrorTracker"/> to track failed delivery attempts.
    /// </summary>
    public interface IErrorTracker
    {
        /// <summary>
        /// Stops tracking the message with the specified ID. If the message is not
        /// being tracked, nothing happens.
        /// </summary>
        /// <param name="id">ID of message to stop tracking</param>
        void StopTracking(string id);

        /// <summary>
        /// Determines whether the message with the specified ID has failed
        /// "enough time"
        /// </summary>
        /// <param name="id">ID of message to check</param>
        /// <returns>Whether the message has failed too many times</returns>
        bool MessageHasFailedMaximumNumberOfTimes(string id);

        /// <summary>
        /// Gets the error messages tracked so far for the message with the specified ID.
        /// </summary>
        /// <param name="id">ID of message whose error messages to get</param>
        /// <returns>Concatenated string of the tracked error messages</returns>
        string GetErrorText(string id);

        /// <summary>
        /// Increments the fail count for this particular message, and starts tracking
        /// the message if it is not already being tracked.
        /// </summary>
        /// <param name="id">ID of the message to track</param>
        /// <param name="exception">The exception that was caught, thus resulting in wanting to track this message</param>
        void TrackDeliveryFail(string id, Exception exception);

        /// <summary>
        /// Returns the fully qualified address of the error queue to which messages should be forwarded in
        /// the event that they exceed the accepted number of failed delivery attempts.
        /// </summary>
        string ErrorQueueAddress { get; }

        /// <summary>
        /// Retrieves the poison message information collected so far for the message with the specified id.
        /// </summary>
        PoisonMessageInfo GetPoisonMessageInfo(string id);

        /// <summary>
        /// Sets the maximum number of retries for some specific exception type
        /// </summary>
        void SetMaxRetriesFor<TException>(int maxRetriesForThisExceptionType) where TException : Exception;
    }

    /// <summary>
    /// Represents a message and information about exceptions that have been caught while attempting
    /// to process the message.
    /// </summary>
    public class PoisonMessageInfo
    {
        /// <summary>
        /// Constructs a poison message info
        /// </summary>
        /// <param name="id">ID of the tracked message</param>
        /// <param name="exceptions">Timed exceptions that were caught wile attempting to process the message</param>
        public PoisonMessageInfo(string id, IEnumerable<Timed<Exception>> exceptions)
        {
            Id = id;
            Exceptions = exceptions.ToArray();
        }

        /// <summary>
        /// The id of the transport message.
        /// </summary>
        public string Id { get; private set; }
        
        /// <summary>
        /// Collection of exceptions caught at specific times while processing the message.
        /// </summary>
        public Timed<Exception>[] Exceptions { get; private set; }
    }
}