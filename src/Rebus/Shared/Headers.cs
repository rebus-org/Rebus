namespace Rebus.Shared
{
    /// <summary>
    /// Contains keys of headers with special meaning in Rebus.
    /// </summary>
    public class Headers
    {
        /// <summary>
        /// Specifies that the contents of the message has been encrypted.
        /// </summary>
        public const string Encrypted = "rebus-encrypted";

        /// <summary>
        /// Key of header that contains the salt that was used when encrypting this message
        /// </summary>
        public const string EncryptionSalt = "rebus-salt";

        /// <summary>
        /// Key of header that contains the unique ID of the message.
        /// </summary>
        public const string MessageId = "rebus-msg-id";

        /// <summary>
        /// Key of header that specifies the return address of a message.
        /// </summary>
        public const string ReturnAddress = "rebus-return-address";

        /// <summary>
        /// Key of header that specifies the original return address of a message. This can be used
        /// to specify the original sender's address in cases where a message is forwarded on another
        /// service's behalf.
        /// </summary>
        public const string OriginalReturnAddress = "rebus-original-return-address";

        /// <summary>
        /// Key of header that contains an error message that stems from someone having experienced bad things trying to handle this message.
        /// </summary>
        public const string ErrorMessage = "rebus-error-message";

        /// <summary>
        /// Key of header that specifies the maximum time a sent/published message is valid. This can/should be used by the infrastructure
        /// to allow messages to expire when they are no longer relevant.
        /// </summary>
        public const string TimeToBeReceived = "rebus-time-to-be-received";

        /// <summary>
        /// Key of source queue name - is attached to poison messages when they are moved to the error queue, allowing
        /// someone to re-deliver the message when the receiver is ready to retry.
        /// </summary>
        public const string SourceQueue = "rebus-source-queue";

        /// <summary>
        /// Specifies the type of the content included in the body of the message.
        /// </summary>
        public const string ContentType = "rebus-content-type";

        /// <summary>
        /// In the event that the content is some kind of string, this header indicates which encoding was used
        /// when serializing the string.
        /// </summary>
        public const string Encoding = "rebus-encoding";

        /// <summary>
        /// Indicates that this message may be delivered faster if it is possible, most likely at the expense of
        /// delivery guarantee. E.g. a message queue might not durably persist the message when this header is
        /// added, which might lead to message loss in the event of a server crash.
        /// </summary>
        public const string Express = "rebus-express";

        /// <summary>
        /// Indicates thats the message will be sent using some kind of multicast protocol. This might lead transport
        /// implementations to behave differently.
        /// </summary>
        public const string Multicast = "rebus-multicast";

        /// <summary>
        /// When a message is sent from within a saga, the saga's ID is attached to the outgoing
        /// message in order to support auto-correlating replies back to the requesting saga.
        /// </summary>
        public const string AutoCorrelationSagaId = "rebus-autocorrelation-saga-id";

        /// <summary>
        /// Special header that will flow through message handlers and be automatically transferred to all outgoing messages.
        /// </summary>
        public const string CorrelationId = "rebus-correlation-id";
    }
}