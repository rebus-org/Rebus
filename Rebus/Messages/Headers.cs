using System;

namespace Rebus.Messages
{
    /// <summary>
    /// Contains keys of headers known & used by Rebus
    /// </summary>
    public class Headers
    {
        /// <summary>
        /// Id of the message. Either set the ID explicitly when sending a message, or Rebus will assign one to the message.
        /// </summary>
        public const string MessageId = "rbs2-msg-id";
        
        /// <summary>
        /// .NET type (if possible) of the sent message 
        /// </summary>
        public const string Type = "rbs2-msg-type";

        /// <summary>
        /// Any messages sent/forwarded/replied/published while handling a message will get the correlation ID (or the message ID
        /// if there's no correlation ID) of the handled message copied to it. When a message is initially sent, its correlation ID
        /// will be its own message ID.
        /// </summary>
        public const string CorrelationId = "rbs2-corr-id";

        /// <summary>
        /// The return address of the message, i.e. the address that repliers must reply to.
        /// </summary>
        public const string ReturnAddress = "rbs2-return-address";

        /// <summary>
        /// Describes the contents of the message with a type and an encoding
        /// </summary>
        public const string ContentType = "rbs2-content-type";

        /// <summary>
        /// Details that can be attached to a message that is forwarded after it has failed
        /// </summary>
        public const string ErrorDetails = "rbs2-error-details";

        /// <summary>
        /// Source queue of a message the has bee forwarded to an error queue after it has failed
        /// </summary>
        public const string SourceQueue = "rbs2-source-queue";

        /// <summary>
        /// Indicates that the message must not be consumed right away, delivery should be delayed until the specified time
        /// </summary>
        public const string DeferredUntil = "rbs2-deferred-until";

        /// <summary>
        /// Indicates a time span (as a string, on the form hh:MM:ss) after which the queueing system can safely delete the message and thus never deliver it
        /// </summary>
        public const string TimeToBeReceived = "rbs2-time-to-be-received";

        /// <summary>
        /// Header that indicates that the queueing system can trade reliability for performance in order to deliver this message as fast as possible
        /// </summary>
        public const string Express = "rbs2-express";

        /// <summary>
        /// Headers with <see cref="DateTimeOffset"/> (serialized with the format string 'O') of the time when the message was sent.
        /// </summary>
        public const string SentTime = "rbs2-senttime";
    }
}