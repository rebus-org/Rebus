using System;
#if NET45
using System.Runtime.Serialization;
#endif

namespace Rebus.Exceptions
{
    /// <summary>
    /// Generic configuration exception to use instead of ConfigurationErrorsException from System.Configuration
    /// </summary>
#if NET45
    [Serializable]
    public class RebusConfigurationException : ApplicationException
# elif NETSTANDARD1_3
    public class RebusConfigurationException : Exception
#endif
    {
        /// <summary>
        /// Constructs the exception with the given message
        /// </summary>
        public RebusConfigurationException(string message)
            :base(message)
        {
        }

        /// <summary>
        /// Constructs the exception with the given message and inner exception
        /// </summary>
        public RebusConfigurationException(Exception innerException, string message)
            :base(message, innerException)
        {
        }

#if NET45
        /// <summary>
        /// Happy cross-domain serialization!
        /// </summary>
        public RebusConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}