using System;

namespace Rebus.Retry.Simple
{
    /// <summary>
    /// Wraps a failed message that is to be retried
    /// </summary>
    public class Failed<TMessage>
    {
        /// <summary>
        /// Gets the message that failed
        /// </summary>
        public TMessage Message { get; private set; }

        /// <summary>
        /// Constructs the wrapper with the given message
        /// </summary>
        public Failed(TMessage message)
        {
            if (message == null) throw new ArgumentNullException("message");
            Message = message;
        }

        /// <summary>
        /// Returns a string that represents the current failed message
        /// </summary>
        public override string ToString()
        {
            return string.Format("FAILED: {0}", Message);
        }
    }
}