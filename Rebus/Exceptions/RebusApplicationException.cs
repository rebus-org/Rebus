using System;
using System.Runtime.Serialization;

namespace Rebus.Exceptions
{
    /// <summary>
    /// Generic application exception to use when something bad happens that is pretty unexpected and should be taken seriously
    /// </summary>
    [Serializable]
    public class RebusApplicationException : Exception
    {
        /// <summary>
        /// Constructs the exception with the given message
        /// </summary>
        public RebusApplicationException(string message, params object[] objs)
            :base(SafeStringFormat(message, objs))
        {
        }

        /// <summary>
        /// Constructs the exception with the given message and inner exception
        /// </summary>
        public RebusApplicationException(Exception innerException, string message, params object[] objs)
            :base(SafeStringFormat(message, objs), innerException)
        {
        }

        static string SafeStringFormat(string message, object[] objs)
        {
            try
            {
                return string.Format(message, objs);
            }
            catch
            {
                return message;
            }
        }

        /// <summary>
        /// Happy cross-domain serialization!
        /// </summary>
        public RebusApplicationException(SerializationInfo info, StreamingContext context)
            :base(info, context)
        {
        }
    }
}