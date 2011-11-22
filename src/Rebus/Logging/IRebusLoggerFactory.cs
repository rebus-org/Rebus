using System;

namespace Rebus.Logging
{
    public interface IRebusLoggerFactory
    {
        ILog GetLogger(Type type);
    }
}