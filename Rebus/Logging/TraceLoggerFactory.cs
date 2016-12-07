using System;
using System.Diagnostics;

namespace Rebus.Logging
{
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
            return new TraceLogger(type);
        }

        public override ILog GetCurrentClassLogger()
        {
            return GetLogger(typeof(TraceLoggerFactory));
        }

        class TraceLogger : ILog
        {
            readonly Type _type;

            public TraceLogger(Type type)
            {
                _type = type;
            }

            public void Debug(string message, params object[] objs)
            {
                Trace.TraceInformation(_type + ": " + message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                Trace.TraceInformation(_type + ": " + message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                Trace.TraceWarning(_type + ": " + message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                Trace.TraceError(_type + ": " + string.Format(message, objs) + Environment.NewLine + exception);
            }

            public void Error(string message, params object[] objs)
            {
                Trace.TraceError(_type + ": " + message, objs);
            }
        }
    }
}