namespace Rebus.Messages
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
        /// Key of header that contains the unique ID of the message.
        /// </summary>
        public const string MessageId = "rebus-msg-id";

        /// <summary>
        /// Key of header that specifies the return address of a message.
        /// </summary>
        public const string ReturnAddress = "rebus-return-address";

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

        public const string ContentType = "rebus-content-type";
        public const string Encoding = "rebus-encoding";
    }
}