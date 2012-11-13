using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Transports;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.RabbitMQ
{
    public static class RabbitMqConfigurationExtensions
    {
        /// <summary>
        /// Configures the bus to use RabbitMQ as a send-only transport - i.e. the bus will only be able to send messages, and if RabbitMQ manages
        /// subscriptions it will be able to publish as well. Will connect to the Rabbit server specified by the supplied connection string. An 
        /// exchange called "Rebus" will automatically be created, which will be used to send all messages.
        /// </summary>
        public static RabbitMqOptions UseRabbitMqInOneWayMode(this RebusTransportConfigurer configurer, string connectionString)
        {
            return DoItOneWay(configurer, connectionString, true);
        }

        /// <summary>
        /// Configures the bus to use RabbitMQ as a send-only transport - i.e. the bus will only be able to send messages, and if RabbitMQ manages
        /// subscriptions it will be able to publish as well. Will connect to the Rabbit server specified by the supplied connection string. This 
        /// configuration assumes that an exchange already exists named "Rebus", which will be used to send all messages.
        /// </summary>
        public static RabbitMqOptions UseRabbitMqInOneWayModeWithExistingExchange(this RebusTransportConfigurer configurer, string connectionString)
        {
            return DoItOneWay(configurer, connectionString, false);
        }

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

        static RabbitMqOptions DoItOneWay(RebusTransportConfigurer configurer, string connectionString, bool ensureExchangeIsDeclared)
        {
            var messageQueue = RabbitMqMessageQueue.Sender(connectionString, ensureExchangeIsDeclared);
            configurer.UseSender(messageQueue);

            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);

            return new RabbitMqOptions(messageQueue, configurer);
        }

        static RabbitMqOptions DoItWithAppConfig(RebusTransportConfigurer configurer, string connectionString, bool ensureExchangeIsDeclared)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                section.VerifyPresenceOfInputQueueConfig();
                section.VerifyPresenceOfErrorQueueConfig();

                var inputQueueName = section.InputQueue;
                var errorQueueName = section.ErrorQueue;

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