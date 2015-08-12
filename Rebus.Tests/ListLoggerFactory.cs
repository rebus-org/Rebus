using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Logging;

namespace Rebus.Tests
{
    public class ListLoggerFactory : AbstractRebusLoggerFactory, IEnumerable<LogLine>
    {
        readonly ConcurrentQueue<LogLine> _loggedLines = new ConcurrentQueue<LogLine>();

        protected override ILog GetLogger(Type type)
        {
            return new ListLogger(_loggedLines, type);
        }

        public IEnumerator<LogLine> GetEnumerator()
        {
            return _loggedLines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class ListLogger : ILog
        {
            readonly ConcurrentQueue<LogLine> _loggedLines;
            readonly Type _type;

            public ListLogger(ConcurrentQueue<LogLine> loggedLines, Type type)
            {
                _loggedLines = loggedLines;
                _type = type;
            }

            public void Debug(string message, params object[] objs)
            {
                Append(LogLevel.Debug, message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                Append(LogLevel.Info, message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                Append(LogLevel.Warn, message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                var text = SafeFormat(message, objs);
                Append(LogLevel.Error, "{0}: {1}", text, exception);
            }

            public void Error(string message, params object[] objs)
            {
                Append(LogLevel.Error, message, objs);
            }

            void Append(LogLevel level, string message, params object[] objs)
            {
                _loggedLines.Enqueue(new LogLine(level, SafeFormat(message, objs), _type));
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

    public class LogLine
    {
        public DateTime Time { get; private set; }
        public LogLevel Level { get; private set; }
        public Type Type { get; private set; }
        public string Text { get; private set; }

        public LogLine(LogLevel level, string text, Type type)
        {
            Time = DateTime.Now;
            Level = level;
            Text = text;
            Type = type;
        }

        public override string ToString()
        {
            return string.Format("{0} / {1} / {2}", Level, Type, string.Join(" | ", Text.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)));
        }
    }
}