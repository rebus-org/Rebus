using Rebus.Configuration.Configurers;
using Rebus.Logging;

namespace Rebus.Log4Net
{
    public static class Log4NetLoggingExtension
    {
        public static void Log4Net(this LoggingConfigurer configurer)
        {
            RebusLoggerFactory.Current = new Log4NetLoggerFactory();
        }
    }
}