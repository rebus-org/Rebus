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

    /// <summary>
    /// Allows for fluently configuring RabbitMQ options
    /// </summary>
    public class RabbitMqOptionsBuilder
    {
        /// <summary>
        /// Sets max for how many messages the RabbitMQ driver should download in the background
        /// </summary>
        public RabbitMqOptionsBuilder SetPrefetch(int numberOfMessagesToprefetch)
        {
            NumberOfMessagesToprefetch = numberOfMessagesToprefetch;
            return this;
        }

        internal int? NumberOfMessagesToprefetch { get; set; }
    }
}