using System;

namespace Rebus.Logging;

/// <summary>
/// This is the /dev/null of loggers...
/// </summary>
public class NullLoggerFactory : AbstractRebusLoggerFactory
{
    static readonly NullLogger Logger = new NullLogger();

    /// <summary>
    /// Returns a <see cref="NullLogger"/> which is the /dev/null of logging...
    /// </summary>
    protected override ILog GetLogger(Type type)
    {
        return Logger;
    }

    class NullLogger : ILog
    {
        public void Debug(string message, params object[] objs)
        {
        }

        public void Info(string message, params object[] objs)
        {
        }

        public void Warn(string message, params object[] objs)
        {
        }

        public void Warn(Exception exception, string message, params object[] objs)
        {
        }

        public void Error(Exception exception, string message, params object[] objs)
        {
        }

        public void Error(string message, params object[] objs)
        {
        }
    }
}