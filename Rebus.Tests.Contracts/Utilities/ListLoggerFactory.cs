using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Logging;

namespace Rebus.Tests.Contracts.Utilities;

/// <summary>
/// Implementation of <see cref="IRebusLoggerFactory"/> that collects logs in a <see cref="ConcurrentQueue{T}"/>, enabling inspection after running a test.
/// </summary>
public class ListLoggerFactory : AbstractRebusLoggerFactory, IEnumerable<LogLine>
{
    readonly ConcurrentQueue<LogLine> _logLines = new();
    readonly bool _outputToConsole;
    readonly bool _detailed;

    /// <summary>
    /// Creates the logger factory.
    /// </summary>
    /// <param name="outputToConsole">Specifies whether logged lines should be simultaneously output to the console as they are logged</param>
    /// <param name="detailed">Specifies whether to use the detailed format when outputting to the console (if true, <paramref name="outputToConsole"/>=true is implied)</param>
    public ListLoggerFactory(bool outputToConsole = false, bool detailed = false)
    {
        _outputToConsole = outputToConsole || detailed;
        _detailed = detailed;
    }

    /// <inheritdoc />
    protected override ILog GetLogger( Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        return new ListLogger(_logLines, type, _outputToConsole, _detailed, this);
    }

    /// <summary>
    /// Clears the in-mem list of logged lines
    /// </summary>
    public void Clear()
    {
        while (_logLines.TryDequeue(out _)) { }
        Console.WriteLine("Cleared the logs");
    }

    /// <summary>
    /// Renders the entire collection of log lines into a string
    /// </summary>
    public override string ToString() => string.Join(Environment.NewLine, _logLines);

    /// <summary>
    /// Allows for enumerating the log lines
    /// </summary>
    /// <returns></returns>
    public IEnumerator<LogLine> GetEnumerator() => _logLines.GetEnumerator();

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