using Rebus.Configuration.Configurers;
using Rebus.Logging;

namespace Rebus.NLog
{
    public static class NLogLoggingExtension
    {
        public static void Log4Net(this LoggingConfigurer configurer)
        {
            RebusLoggerFactory.Current = new NLogLoggerFactory();
        }
    }
}
