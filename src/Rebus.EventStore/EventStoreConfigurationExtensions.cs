using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Transports;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.EventStore
{
    public static class EventStoreConfigurationExtensions
    {
       /// <summary>
        /// Configures Rebus to run in one-way client mode, which means that the bus is capable only of sending messages.
        /// </summary>
        public static void UseEventStoreInOneWayClientMode(this RebusTransportConfigurer configurer)
        {
            configurer.UseSender(CreateSender());
            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);   
        }

        /// <summary>
        /// Specifies that you want to use EventStore to both send and receive messages. The input
        /// queue name will be deduced from the Rebus configuration section in the application
        /// configuration file. The input queue will be automatically created if it doesn't exist.
        /// </summary>
        public static void UseEventStoreAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer, string applicationId)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                section.VerifyPresenceOfInputQueueConfig();
                section.VerifyPresenceOfErrorQueueConfig();

                var inputQueueName = section.InputQueue;
                var errorQueueName = section.ErrorQueue;

                if (string.IsNullOrEmpty(inputQueueName))
                {
                    throw new ConfigurationErrorsException("You need to specify an input queue.");
                }

                configurer.UseSender(CreateSender());
                configurer.UseReceiver(CreateReceiver(inputQueueName, applicationId));
                configurer.UseErrorTracker(new ErrorTracker(errorQueueName));
            }
            catch (ConfigurationErrorsException e)
            {
                throw new ConfigurationException(
                    @"
An error occurred when trying to parse out the configuration of the RebusConfigurationSection:

{0}

-

For this way of configuring input queue to work, you need to supply a correct configuration
section declaration in the <configSections> element of your app.config/web.config - like so:

    <configSections>
        <section name=""rebus"" type=""Rebus.Configuration.RebusConfigurationSection, Rebus"" />
        <!-- other stuff in here as well -->
    </configSections>

-and then you need a <rebus> element some place further down the app.config/web.config,
like so:

    <rebus inputQueue=""my.service.input.queue"" errorQueue=""my.service.error.queue"" />

Note also, that specifying the input queue name with the 'inputQueue' attribute is optional.

A more full example configuration snippet can be seen here:

{1}
",
                    e, RebusConfigurationSection.ExampleSnippetForErrorMessages);
            }
        }

        static EventStoreSendMessages CreateSender()
        {
            return new EventStoreSendMessages(EventStoreConnectionManager.CreateConnectionAndWait());
        }

        static EventStoreReceiveMessages CreateReceiver(string inputQueue, string applicationId)
        {
            return new EventStoreReceiveMessages(applicationId, inputQueue, EventStoreConnectionManager.CreateConnectionAndWait());
        }
    }
}
