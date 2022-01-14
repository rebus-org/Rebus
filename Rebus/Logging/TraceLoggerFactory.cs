using System;
using System.Diagnostics;

namespace Rebus.Logging;

/// <summary>
/// Logger factory that writes log statements using the <see cref="Trace"/> API
/// </summary>
public class TraceLoggerFactory : AbstractRebusLoggerFactory
{
    /// <summary>
    /// Gets a <see cref="TraceLogger"/>
    /// </summary>
    protected override ILog GetLogger(Type type)
    {
        return new TraceLogger(type, this);
    }

    class TraceLogger : ILog
    {
        readonly Type _type;
        readonly TraceLoggerFactory _loggerFactory;

        public TraceLogger(Type type, TraceLoggerFactory loggerFactory)
        {
            _type = type;
            _loggerFactory = loggerFactory;
        }

        public void Debug(string message, params object[] objs)
        {
            Trace.TraceInformation(_type + ": " + _loggerFactory.RenderString(message, objs));
        }

        public void Info(string message, params object[] objs)
        {
            Trace.TraceInformation(_type + ": " + _loggerFactory.RenderString(message, objs));
        }

        public void Warn(string message, params object[] objs)
        {
            Trace.TraceWarning(_type + ": " + _loggerFactory.RenderString(message, objs));
        }

        public void Warn(Exception exception, string message, params object[] objs)
        {
            Trace.TraceWarning(_type + ": " + _loggerFactory.RenderString(message, objs) + Environment.NewLine + exception);
        }

        public void Error(Exception exception, string message, params object[] objs)
        {
            Trace.TraceError(_type + ": " + _loggerFactory.RenderString(message, objs) + Environment.NewLine + exception);
        }

        public void Error(string message, params object[] objs)
        {
            Trace.TraceError(_type + ": " + _loggerFactory.RenderString(message, objs));
        }
    }
}