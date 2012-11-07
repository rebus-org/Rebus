using System;

namespace Rebus
{
    /// <summary>
    /// Exception that is thrown when an incoming message cannot be handled by any handlers. This does
    /// not include the case where a message cannot be dispatched to a saga because a saga instance
    /// could not be found.
    /// </summary>
    [Serializable]
    public class UnhandledMessageException : ApplicationException
    {
        readonly object unhandledMessage;

        /// <summary>
        /// Constructs the exception with a reference to the message that could not be handled
        /// </summary>
        public UnhandledMessageException(object unhandledMessage)
            : base(string.Format("Could not find any handlers to execute message of type {0}", unhandledMessage.GetType()))
        {
            this.unhandledMessage = unhandledMessage;
        }

        public object UnhandledMessage
        {
            get { return unhandledMessage; }
        }
    }
}