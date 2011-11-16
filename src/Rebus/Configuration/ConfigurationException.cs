using System;

namespace Rebus.Configuration
{
    public class ConfigurationException : ApplicationException
    {
        public ConfigurationException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }
    }
}