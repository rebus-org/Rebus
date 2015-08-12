using Rebus.Config;
using Rebus.Transport;

namespace Rebus.RabbitMq
{
    /// <summary>
    /// Configuration extensions for the RabbitMQ transport
    /// </summary>
    public static class RabbitMqConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use RabbitMQ to move messages around
        /// </summary>
        public static RabbitMqOptionsBuilder UseRabbitMq(this StandardConfigurer<ITransport> configurer,  string connectionString, string inputQueueName)
        {
            var options = new RabbitMqOptionsBuilder();

            configurer.Register(c =>
            {
                var transport = new RabbitMqTransport(connectionString, inputQueueName);

                if (options.NumberOfMessagesToprefetch.HasValue)
                {
                    transport.SetPrefetching(options.NumberOfMessagesToprefetch.Value);
                }

                return transport;
            });

            return options;
        }
    }
}