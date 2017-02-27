using System;
#if NET45
using System.Runtime.Serialization;
#endif

namespace Rebus.Exceptions
{
    /// <summary>
    /// Special exception that signals that some kind of configuration error has occured.
    /// </summary>
#if NET45
    [Serializable]
    public class ConfigurationErrorsException : ApplicationException
# elif NETSTANDARD1_6
    public class ConfigurationErrorsException : Exception
#endif
    {
        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ConfigurationErrorsException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }

        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ConfigurationErrorsException(Exception innerException, string message, params object[] objs)
            : base(string.Format(message, objs), innerException)
        {
        }

#if NET45
        /// <summary>
        /// Constructs the exception
        /// </summary>
        public ConfigurationErrorsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}