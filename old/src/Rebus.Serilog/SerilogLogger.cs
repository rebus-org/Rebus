using System;
using Serilog;
using RebusLog = Rebus.Logging.ILog;

namespace Rebus.Serilog
{
    class SerilogLogger : RebusLog
    {
        readonly ILogger log;

        public SerilogLogger(ILogger log)
        {
            this.log = log;
        }

        public void Debug(string message, params object[] objs)
        {
            log.Debug(string.Format(message, objs));
        }

        public void Info(string message, params object[] objs)
        {
            log.Information(string.Format(message, objs));
        }

        public void Warn(string message, params object[] objs)
        {
            log.Warning(string.Format(message, objs));
        }

        public void Error(Exception exception, string message, params object[] objs)
        {
            try
            {
                log.Error(exception, string.Format(message, objs));
            }
            catch
            {
                log.Warning("Could not render string with arguments: {message}", message);
                log.Error(exception, message);
            }
        }

        public void Error(string message, params object[] objs)
        {
            log.Error(string.Format(message, objs));
        }
    }
}
