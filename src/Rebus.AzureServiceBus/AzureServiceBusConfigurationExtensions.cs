using System;
using System.Configuration;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports;
using ConfigurationException = Rebus.Configuration.ConfigurationException;
using Rebus.Transports.Msmq;
using System.Linq;

namespace Rebus.AzureServiceBus
{
    public static class AzureServiceBusConfigurationExtensions
    {
        static ILog log;

        static AzureServiceBusConfigurationExtensions()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public static void UseAzureServiceBus(this RebusTransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueueName)
        {
            Configure(configurer, connectionString, inputQueueName, errorQueueName);
        }

        public static void UseAzureServiceBusInOneWayClientMode(this RebusTransportConfigurer configurer,
                                                                string connectionString)
        {
            if (ShouldEmulateAzureEnvironment(connectionString))
            {
                var sender = MsmqMessageQueue.Sender();
                configurer.UseSender(sender);
            }
            else
            {
                var sender = AzureServiceBusMessageQueue.Sender(connectionString);
                configurer.UseSender(sender);
            }

            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);
        }

        public static void UseAzureServiceBusAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer, string connectionString)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                section.VerifyPresenceOfInputQueueConfig();
                section.VerifyPresenceOfErrorQueueConfig();

                var inputQueueName = section.InputQueue;
                var errorQueueName = section.ErrorQueue;

                Configure(configurer, connectionString, inputQueueName, errorQueueName);
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

        static void Configure(RebusTransportConfigurer configurer, string connectionString, string inputQueueName,
                              string errorQueueName)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString", "You need to specify a connection string in order to configure Rebus to use Azure Service Bus as the transport. If you want to simulate Azure Service Bus by using MSMQ, you may use 'UseDevelopmentStorage=true' as the connection string.");
            }

            if (ShouldEmulateAzureEnvironment(connectionString))
            {
                log.Info("Azure Service Bus configuration has detected that development storage should be used - for the");
                configurer.UseMsmq(inputQueueName, errorQueueName);
                return;
            }

            var azureServiceBusMessageQueue = new AzureServiceBusMessageQueue(connectionString, inputQueueName);
            configurer.UseSender(azureServiceBusMessageQueue);
            configurer.UseReceiver(azureServiceBusMessageQueue);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));
            azureServiceBusMessageQueue.GetOrCreateSubscription(errorQueueName);
        }

        static bool ShouldEmulateAzureEnvironment(string connectionString)
        {
            var variablePairs = connectionString.Split(';');

            foreach (var pair in variablePairs)
            {
                var tokens = pair.Split('=')
                                 .Select(t => t.Trim())
                                 .ToArray();

                if (tokens.Length == 2)
                {
                    if (tokens[0].Equals("UseDevelopmentStorage", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            return bool.Parse(tokens[1]);
                        }
                        catch (Exception e)
                        {
                            throw new FormatException(
                                string.Format("Could not interpret {0} as a proper bool", tokens[1]), e);
                        }
                    }
                }
            }

            return false;
        }
    }
}