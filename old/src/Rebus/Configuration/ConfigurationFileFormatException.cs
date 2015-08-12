using System;
using System.Runtime.Serialization;

namespace Rebus.Configuration
{
    /// <summary>
    /// Exception that gets thrown when an error is encountered while attempting to parse out
    /// an NServiceBus format configuration from the current app.config
    /// </summary>
    [Serializable]
    public class ConfigurationFileFormatException : ApplicationException
    {
        /// <summary>
        /// Mandatory exception ctor
        /// </summary>
        protected ConfigurationFileFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Constructs this bad boy!
        /// </summary>
        public ConfigurationFileFormatException(string message, params object[] objs)
            : base(string.Format(@"Could not parse configuration file!

{0}", string.Format(message, objs)))
        {
        }
    }
}