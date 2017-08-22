using System;
using System.Runtime.Serialization;

namespace Rebus.Injection
{
    /// <summary>
    /// Exceptions that is thrown when something goes wrong while working with the injectionist
    /// </summary>
#if NET45
    [Serializable]
#endif
    public class ResolutionException : Exception
    {
        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ResolutionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ResolutionException(Exception innerException, string message)
            : base(message, innerException)
        {
        }

#if NET45
        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ResolutionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}