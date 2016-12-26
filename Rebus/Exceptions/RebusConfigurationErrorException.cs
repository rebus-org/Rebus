using System;

namespace Rebus.Exceptions
{
    public class RebusConfigurationErrorException : Exception
    {
        /// <summary>
        /// Constructs the exception with the given message
        /// </summary>
        public RebusConfigurationErrorException(string message)
            :base(message)
        {
        }

        /// <summary>
        /// Constructs the exception with the given message and inner exception
        /// </summary>
        public RebusConfigurationErrorException(Exception innerException, string message)
            :base(message, innerException)
        {
        }
    }
}
