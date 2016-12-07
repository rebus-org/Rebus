using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Rebus.Logging;

namespace Rebus.Testing.Internals
{
    class TestLoggerFactory : AbstractRebusLoggerFactory
    {
        readonly ConcurrentQueue<LogEvent> _logEvents = new ConcurrentQueue<LogEvent>();

        public IEnumerable<LogEvent> LogEvents => _logEvents.ToList();

        protected override ILog GetLogger(Type type)
        {
            return new TestLogger(type, _logEvents);
        }

        public override ILog GetCurrentClassLogger()
        {
            return GetLogger(typeof(TestLoggerFactory));
        }

        class TestLogger : ILog
        {
            readonly Type _type;
            readonly ConcurrentQueue<LogEvent> _logEvents;

            public TestLogger(Type type, ConcurrentQueue<LogEvent> logEvents)
            {
                _type = type;
                _logEvents = logEvents;
            }

            public void Debug(string message, params object[] objs)
            {
                Log(LogLevel.Debug, message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                Log(LogLevel.Info, message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                Log(LogLevel.Warn, message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                Log(LogLevel.Error, message, objs, exception);
            }

            public void Error(string message, params object[] objs)
            {
                Log(LogLevel.Error, message, objs);
            }

            void Log(LogLevel level, string message, object[] objs, Exception exception = null)
            {
                _logEvents.Enqueue(new LogEvent(level, SafeFormat(message, objs), exception, _type));
            }

            static string SafeFormat(string message, object[] objs)
            {
                try
                {
                    return string.Format(message, objs);
                }
                catch
                {
                    return message;
                }
            }
        }
    }
}