using Rebus.Configuration.Configurers;
using Rebus.Logging;

namespace Rebus.NLog
{
    public static class NLogLoggingExtension
    {
        public static void NLog(this LoggingConfigurer configurer)
        {
            RebusLoggerFactory.Current = new NLogLoggerFactory();
        }
    }
}
