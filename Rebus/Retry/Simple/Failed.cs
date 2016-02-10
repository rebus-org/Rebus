using System;
using System.Collections.Generic;

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
        public TMessage Message { get; }

        /// <summary>
        /// Gets a (sometimes pretty long) description of the encountered error(s)
        /// </summary>
        public string ErrorDescription { get; }

        /// <summary>
        /// Gets the headers of the message that failed
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Constructs the wrapper with the given message
        /// </summary>
        public Failed(Dictionary<string, string> headers, TMessage message, string errorDescription)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (errorDescription == null) throw new ArgumentNullException(nameof(errorDescription));
            Headers = headers;
            Message = message;
            ErrorDescription = errorDescription;
        }

        /// <summary>
        /// Returns a string that represents the current failed message
        /// </summary>
        public override string ToString()
        {
            return $"FAILED: {Message}";
        }
    }
}