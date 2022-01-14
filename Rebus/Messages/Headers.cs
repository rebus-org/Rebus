using System;
using Rebus.Bus.Advanced;

namespace Rebus.Messages;

/// <summary>
/// Contains keys of headers known &amp; used by Rebus
/// </summary>
public static class Headers
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
    /// Contains the <see cref="MessageId"/> from the handled message when replying from a handler
    /// </summary>
    public const string InReplyTo = "rbs2-in-reply-to";

    /// <summary>
    /// Any messages sent/forwarded/replied/published while handling a message will get a correlation sequence number of the handled message 
    /// incremented by 1 copied to it. When a message is initially sent, its correlation sequence number is 0. The sequence number
    /// can be used to deduce a strict ordering of correlated messages, even in the face of clock skew among servers
    /// </summary>
    public const string CorrelationSequence = "rbs2-corr-seq";

    /// <summary>
    /// The return address of the message, i.e. the address that repliers must reply to.
    /// </summary>
    public const string ReturnAddress = "rbs2-return-address";

    /// <summary>
    /// Address of the message sender, if the sender is not a one-way client.
    /// </summary>
    public const string SenderAddress = "rbs2-sender-address";

    /// <summary>
    /// A ;-separated list of addresses that the routing slip must visit
    /// </summary>
    public const string RoutingSlipItinerary = "rbs2-itinerary";

    /// <summary>
    /// A ;-separated list of addresses that the routing slip has visited
    /// </summary>
    public const string RoutingSlipTravelogue = "rbs2-travelogue";

    /// <summary>
    /// Describes the contents of the message with a type and an encoding
    /// </summary>
    public const string ContentType = "rbs2-content-type";

    /// <summary>
    /// Optional header element that specifies an encoding that the content is encoded with, e.g. gzip
    /// </summary>
    public const string ContentEncoding = "rbs2-content-encoding";

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
    /// Indicates to which input queue the deferred message must be delivered back to
    /// </summary>
    public const string DeferredRecipient = "rbs2-defer-recipient";

    /// <summary>
    /// Indicates how many times the message has been deferred. Will initially be set to 1, and will then be incremented each time the transport message
    /// is deferred with <see cref="ITransportMessageApi.Defer"/>.
    /// </summary>
    public const string DeferCount = "rbs2-defer-count";
        
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

    /// <summary>
    /// Header that indicates whether this message was sent or published
    /// </summary>
    public const string Intent = "rbs2-intent";

    /// <summary>
    /// Contains the possible values for the <see cref="Headers.Intent"/> header
    /// </summary>
    public class IntentOptions
    {
        /// <summary>
        /// This value indicates that the message was sent to one specific recipient, i.e. either by sending or replying
        /// </summary>
        public const string PointToPoint = "p2p";

        /// <summary>
        /// This value indicates that the message was published to zero or more recipients, i.e. it might not actually be received by anyone.
        /// When auditing is enabled, a copy is always stored of published messages, regardless of the number of recipients.
        /// </summary>
        public const string PublishSubscribe = "pub";
    }

    /// <summary>
    /// Header that contains the ID of a data bus attachment, which was used to store the message body
    /// </summary>
    public const string MessagePayloadAttachmentId = "rbs2-payload-attachment-id";
}