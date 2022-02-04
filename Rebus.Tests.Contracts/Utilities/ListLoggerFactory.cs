using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rebus.Logging;

namespace Rebus.Tests.Contracts.Utilities;

public class ListLoggerFactory : AbstractRebusLoggerFactory, IEnumerable<LogLine>
{
    readonly bool _outputToConsole;
    readonly bool _detailed;

    public ListLoggerFactory(bool outputToConsole = false, bool detailed = false)
    {
        _outputToConsole = outputToConsole || detailed;
        _detailed = detailed;
    }

    protected override ILog GetLogger([NotNull] Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        return new ListLogger(LogLines, type, _outputToConsole, _detailed, this);
    }

    public ConcurrentQueue<LogLine> LogLines { get; } = new();

    public void Clear()
    {
        while (LogLines.TryDequeue(out _)) { }
        Console.WriteLine("Cleared the logs");
    }

    public override string ToString() => string.Join(Environment.NewLine, LogLines);

    public IEnumerator<LogLine> GetEnumerator() => LogLines.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    class ListLogger : ILog
    {
        readonly ConcurrentQueue<LogLine> _loggedLines;
        readonly Type _type;
        readonly bool _outputToConsole;
        readonly bool _detailed;
        readonly ListLoggerFactory _loggerFactory;

        public ListLogger(ConcurrentQueue<LogLine> loggedLines, Type type, bool outputToConsole, bool detailed, ListLoggerFactory loggerFactory)
        {
            _loggedLines = loggedLines;
            _type = type;
            _outputToConsole = outputToConsole;
            _detailed = detailed;
            _loggerFactory = loggerFactory;
        }

        public void Debug(string message, params object[] objs) => Append(LogLevel.Debug, message, objs);

        public void Info(string message, params object[] objs) => Append(LogLevel.Info, message, objs);

        public void Warn(string message, params object[] objs) => Append(LogLevel.Warn, message, objs);

        public void Warn(Exception exception, string message, params object[] objs) => Append(LogLevel.Warn, "{0}: {1}", _loggerFactory.RenderString(message, objs), exception);

        public void Error(string message, params object[] objs) => Append(LogLevel.Error, message, objs);

        public void Error(Exception exception, string message, params object[] objs) => Append(LogLevel.Error, "{0}: {1}", _loggerFactory.RenderString(message, objs), exception);

        void Append(LogLevel level, string message, params object[] objs)
        {
            var renderedMessage = _loggerFactory.RenderString(message, objs);

            if (_outputToConsole)
            {
                if (_detailed)
                {
                    var now = DateTime.Now;

                    Console.WriteLine($"{now:HH:mm:ss} {level}: {renderedMessage}");
                }
                else
                {
                    Console.WriteLine($"{level}: {renderedMessage}");
                }
            }


            _loggedLines.Enqueue(new LogLine(level, renderedMessage, _type));
        }
    }
}