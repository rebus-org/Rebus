using System;

namespace Rebus.Exceptions
{
    public class ConfigurationErrorsException : Exception
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
    }
}