using System;

namespace Rebus.Logging
{
    /// <summary>
    /// This is a fake stub implementation just to satisfy the compiler (because we're linking in MsmqUtils)
    /// </summary>
    public interface ILog
    {
        void Debug(string message, params object[] objs);
        void Info(string message, params object[] objs);
        void Warn(string message, params object[] objs);
        void Error(Exception exception, string message, params object[] objs);
        void Error(string message, params object[] objs);
    }

    /// <summary>
    /// This is a fake stub implementation just to satisfy the compiler (because we're linking in MsmqUtils)
    /// </summary>
    public class RebusLoggerFactory
    {
        public static event Action<RebusLoggerFactory> Changed = delegate { };

        public ILog GetCurrentClassLogger()
        {
            return null;
        }
    }
}