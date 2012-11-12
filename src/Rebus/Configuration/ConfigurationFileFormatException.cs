using System;

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
        /// Constructs this bad boy!
        /// </summary>
        public ConfigurationFileFormatException(string message, params object[] objs)
            : base(string.Format(@"Could not parse configuration file!

{0}", string.Format(message, objs)))
        {
        }
    }
}