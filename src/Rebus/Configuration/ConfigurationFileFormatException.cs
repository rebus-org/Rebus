using System;

namespace Rebus.Configuration
{
    public class ConfigurationFileFormatException : FormatException
    {
        public ConfigurationFileFormatException(string message, params object[] objs)
            : base(string.Format(@"Could not parse configuration file!

{0}", string.Format(message, objs)))
        {
        }
    }
}