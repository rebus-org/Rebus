namespace Rebus.Messages
{
    public class Headers
    {
        /// <summary>
        /// Key of header that specifies the return address of a message.
        /// </summary>
        public const string ReturnAddress = "returnAddress";

        /// <summary>
        /// Key of header that contains an error message that stems from someone having experienced bad things trying to handle this message.
        /// </summary>
        public const string ErrorMessage = "errorMessage";

        /// <summary>
        /// Key of header that specifies the maximum time a sent/published message is valid. This can/should be used by the infrastructure
        /// to allow messages to expire when they are no longer relevant.
        /// </summary>
        public const string TimeToBeReceived = "timeToBeReceived";
    }
}