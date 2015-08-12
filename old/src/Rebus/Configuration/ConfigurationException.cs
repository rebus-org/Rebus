using System;
using System.Runtime.Serialization;

namespace Rebus.Configuration
{
    /// <summary>
    /// Exception that should be thrown in the event that a given configuration is somehow invalid.
    /// </summary>
    [Serializable]
    public class ConfigurationException : ApplicationException
    {
        /// <summary>
        /// Mandatory exception ctor
        /// </summary>
        protected ConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Constructs the exception with the specified message.
        /// </summary>
        public ConfigurationException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }
    }
}