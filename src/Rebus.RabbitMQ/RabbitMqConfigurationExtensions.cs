using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.RabbitMQ
{
    public static class RabbitMqConfigurationExtensions
    {
        /// <summary>
        /// Configures the bus to use RabbitMQ as the transport. Will connect to the Rabbit server specified by the supplied connection string,
        /// and will use the supplied queue names. An exchange called "Rebus" will automatically be created, which will be used to send all messages.
        /// </summary>
        public static RabbitMqOptions UseRabbitMq(this RebusTransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueue)
        {
            return DoIt(configurer, connectionString, inputQueueName, errorQueue, true);
        }

        /// <summary>
        /// Configures the bus to use RabbitMQ as the transport. Will connect to the Rabbit server specified by the supplied connection string,
        /// and will use the supplied queue names. This configuration assumes that an exchange already exists named "Rebus", which will be used to send all messages.
        /// </summary>
        public static RabbitMqOptions UseRabbitMqWithExistingExchange(this RebusTransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueue)
        {
            return DoIt(configurer, connectionString, inputQueueName, errorQueue, false);
        }

        /// <summary>
        /// Configures the bus to use RabbitMQ as the transport. Will connect to the Rabbit server specified by the supplied connection string,
        /// and will look up Rebus-specified settings in the Rebus configuration section in your app.config. An exchange called "Rebus" will
        /// automatically be created, which will be used to send all messages.
        /// </summary>
        public static RabbitMqOptions UseRabbitMqAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer, string connectionString)
        {
            return DoItWithAppConfig(configurer, connectionString, true);
        }

        /// <summary>
        /// Configures the bus to use RabbitMQ as the transport. Will connect to the Rabbit server specified by the supplied connection string,
        /// and will look up Rebus-specified settings in the Rebus configuration section in your app.config. This configuration assumes that
        /// an exchange already exists named "Rebus", which will be used to send all messages.
        /// </summary>
        public static RabbitMqOptions UseRabbitMqWithExistingExchangeAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer, string connectionString)
        {
            return DoItWithAppConfig(configurer, connectionString, false);
        }

        static RabbitMqOptions DoItWithAppConfig(RebusTransportConfigurer configurer, string connectionString, bool ensureExchangeIsDeclared)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                var inputQueueName = section.InputQueue;

                if (string.IsNullOrEmpty(inputQueueName))
                {
                    throw new ConfigurationErrorsException(
                        "Could not get input queue name from Rebus configuration section. Did you forget the 'inputQueue' attribute?");
                }

                var errorQueueName = section.ErrorQueue;

                if (string.IsNullOrEmpty(errorQueueName))
                {
                    throw new ConfigurationErrorsException(
                        "Could not get input queue name from Rebus configuration section. Did you forget the 'errorQueue' attribute?");
                }

                return DoIt(configurer, connectionString, inputQueueName, errorQueueName, ensureExchangeIsDeclared);
            }
            catch (ConfigurationErrorsException e)
            {
                throw new ConfigurationException(
                    @"
An error occurred when trying to parse out the configuration of the RebusConfigurationSection:

{0}

-

For this way of configuring input queue to work, you need to supply a correct configuration section declaration in the <configSections> element of your app.config/web.config - like so:

    <configSections>
        <section name=""rebus"" type=""Rebus.Configuration.RebusConfigurationSection, Rebus"" />
        <!-- other stuff in here as well -->
    </configSections>

-and then you need a <rebus> element some place further down the app.config/web.config, like so:

    <rebus inputQueue=""my.service.input.queue"" errorQueue=""my.service.error.queue"" />

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }
        }

        static RabbitMqOptions DoIt(RebusTransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueueName, bool ensureExchangeIsDeclared)
        {
            var queue = new RabbitMqMessageQueue(connectionString, inputQueueName, ensureExchangeIsDeclared);
            
            configurer.UseSender(queue);
            configurer.UseReceiver(queue);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));
            
            return new RabbitMqOptions(queue, configurer);
        }
    }
}