using Rebus.Configuration.Configurers;

namespace Rebus.Logging
{
    public static class ConsoleLoggerExtensions
    {
         public static void ConsoleLogger(this LoggingConfigurer configurer)
         {
             RebusLoggerFactory.Reset();
         }
    }
}