using System;

namespace Rebus.Exceptions
{
    public class RebusConfigurationException : ApplicationException
    {
        public RebusConfigurationException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }
    }
}