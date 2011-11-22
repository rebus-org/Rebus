using System;

namespace Rebus.Logging
{
    public interface ILog
    {
        void Debug(string message, params object[] objs);
        void Info(string message, params object[] objs);
        void Warn(string message, params object[] objs);
        void Error(Exception exception, string message, params object[] objs);
        void Error(string message, params object[] objs);
        void Fatal(Exception exception, string message, params object[] objs);
        void Fatal(string message, params object[] objs);
    }
}