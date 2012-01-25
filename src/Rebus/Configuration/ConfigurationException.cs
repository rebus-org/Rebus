using System;

namespace Rebus.Configuration
{
    /// <summary>
    /// Exception that should be thrown in the event that a given configuration is somehow invalid.
    /// </summary>
    public class ConfigurationException : ApplicationException
    {
        /// <summary>
        /// Constructs the exception with the specified message.
        /// </summary>
        public ConfigurationException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }
    }
}