using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Persistence.SqlServer;

namespace Rebus.Transports.Msmq
{
    public static class MsmqConfiguration
    {
        public static IBusConfigurer UseMsmqTransport(this IBusConfigurer configurer, string inputQueue)
        {
            return configurer
                .WithValue(ConfigurationKeys.Transport, "msmq")
                .WithValue(ConfigurationKeys.MsmqInputQueue, GenerateMsmqQueueName(inputQueue));
        }

        static string GenerateMsmqQueueName(string inputQueue)
        {
            var tokens = inputQueue.Split('@');
            
            switch (tokens.Length)
            {
                case 1:
                    return GetName(".", tokens[0]);

                case 2:
                    return GetName(tokens[0], tokens[1]);

                default:
                    throw new RebusConfigurationException(
                        @"Input queue '{0}' is invalid.

Use queue names on the form 'someQueueName' to specify a local private queue, and on
the form 'someQueueName@someMachineName' or 'someQueueName@someIP' to specify remote
private queues.",
                        inputQueue);
            }
        }

        static string GetName(string machineName, string queueName)
        {
            return string.Format(@"{0}\private$\{1}", machineName, queueName);
        }
    }
}