using System;
using System.Runtime.Serialization;

namespace Rebus.Injection
{
    /// <summary>
    /// Exceptions that is thrown when something goes wrong while working with the injectionist
    /// </summary>
    [Serializable]
    public class ResolutionException : Exception
    {
        public ResolutionException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }

        public ResolutionException(Exception innerException, string message, params object[] objs)
            : base(string.Format(message, objs), innerException)
        {
        }

        public ResolutionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}