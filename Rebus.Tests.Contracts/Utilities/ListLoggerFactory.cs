using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Logging;

namespace Rebus.Tests.Contracts.Utilities
{
    public class ListLoggerFactory : AbstractRebusLoggerFactory, IEnumerable<LogLine>
    {
        readonly ConcurrentQueue<LogLine> _loggedLines = new ConcurrentQueue<LogLine>();
        readonly bool _outputToConsole;
        readonly bool _detailed;

        public ListLoggerFactory(bool outputToConsole = false, bool detailed = false)
        {
            _outputToConsole = outputToConsole || detailed;
            _detailed = detailed;
        }

        public void Clear()
        {
            LogLine temp;
            while (_loggedLines.TryDequeue(out temp)) { }
            Console.WriteLine("Cleared the logs");
        }

        protected override ILog GetLogger(Type type)
        {
            return new ListLogger(_loggedLines, type, _outputToConsole, _detailed);
        }

        public override ILog GetCurrentClassLogger()
        {
            return GetLogger(typeof(ListLoggerFactory));
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
            readonly bool _outputToConsole;
            readonly bool _detailed;

            public ListLogger(ConcurrentQueue<LogLine> loggedLines, Type type, bool outputToConsole, bool detailed)
            {
                _loggedLines = loggedLines;
                _type = type;
                _outputToConsole = outputToConsole;
                _detailed = detailed;
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
                if (_outputToConsole)
                {
                    if (_detailed)
                    {
                        var now = DateTime.Now;

                        Console.WriteLine($"{now:HH:mm:ss} {level}: {string.Format(message, objs)}");
                    }
                    else
                    {
                        Console.WriteLine($"{level}: {string.Format(message, objs)}");
                    }
                }

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
}