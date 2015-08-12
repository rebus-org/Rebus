using System;
using System.Configuration;
using System.Linq;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Transports;
using Rebus.Transports.Msmq;
using ConfigurationException = Rebus.Configuration.ConfigurationException;

namespace Rebus.AzureServiceBus
{
    /// <summary>
    /// Configuration extensions for configuring Rebus to use Azure Service Bus QUEUES as its transport.
    /// </summary>
    public static class AzureServiceBusConfigurationExtensions
    {
        static ILog log;

        static AzureServiceBusConfigurationExtensions()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public static IAsbOptions UseAzureServiceBus(this RebusTransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueueName)
        {
            return Configure(configurer, connectionString, inputQueueName, errorQueueName);
        }

        public static IAsbOptions UseAzureServiceBusInOneWayClientMode(this RebusTransportConfigurer configurer,
                                                                string connectionString)
        {
            IAsbOptions asbOptionsToReturn;

            if (ShouldEmulateAzureEnvironment(connectionString))
            {
                var sender = MsmqMessageQueue.Sender();
                configurer.UseSender(sender);
                asbOptionsToReturn = new NoopAsbOptions();
            }
            else
            {
                var sender = AzureServiceBusMessageQueue.Sender(connectionString);
                configurer.UseSender(sender);
                asbOptionsToReturn = new AsbOptions(sender);
            }

            var gag = new OneWayClientGag();
            configurer.UseReceiver(gag);
            configurer.UseErrorTracker(gag);

            return asbOptionsToReturn;
        }

        public static IAsbOptions UseAzureServiceBusAndGetInputQueueNameFromAppConfig(this RebusTransportConfigurer configurer, string connectionString)
        {
            try
            {
                var section = RebusConfigurationSection.LookItUp();

                section.VerifyPresenceOfInputQueueConfig();
                section.VerifyPresenceOfErrorQueueConfig();

                var inputQueueName = section.InputQueue;
                var errorQueueName = section.ErrorQueue;

                return Configure(configurer, connectionString, inputQueueName, errorQueueName);
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

        static IAsbOptions Configure(RebusTransportConfigurer configurer, string connectionString, string inputQueueName, string errorQueueName)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString", "You need to specify a connection string in order to configure Rebus to use Azure Service Bus as the transport. If you want to simulate Azure Service Bus by using MSMQ, you may use 'UseDevelopmentStorage=true' as the connection string.");
            }

            if (ShouldEmulateAzureEnvironment(connectionString))
            {
                log.Info("Azure Service Bus configuration has detected that development storage should be used - for the");
                configurer.UseMsmq(inputQueueName, errorQueueName);
                // when we're emulating with MSMQ, we make this noop action available to allow user code to pretend to renew the peek lock
                configurer
                    .Backbone
                    .ConfigureEvents(e =>
                    {
                        e.MessageContextEstablished += (bus, context) =>
                        {
                            var noop = (Action)(() => log.Info("Azure Service Bus message peek lock would be renewed at this time"));

                            context.Items[AzureServiceBusMessageQueue.AzureServiceBusRenewLeaseAction] = noop;
                        };
                    });
                return new NoopAsbOptions();
            }

            var azureServiceBusMessageQueue = new AzureServiceBusMessageQueue(connectionString, inputQueueName);
            configurer.UseSender(azureServiceBusMessageQueue);
            configurer.UseReceiver(azureServiceBusMessageQueue);
            configurer.UseErrorTracker(new ErrorTracker(errorQueueName));

            azureServiceBusMessageQueue.EnsureQueueExists(errorQueueName);

            // transfer renew-peek-lock-action from transaction context to message context
            configurer
                .Backbone
                .ConfigureEvents(e =>
                {
                    e.MessageContextEstablished += (bus, context) =>
                    {
                        var renewAction = TransactionContext.Current[AzureServiceBusMessageQueue.AzureServiceBusRenewLeaseAction];
                        
                        context.Items[AzureServiceBusMessageQueue.AzureServiceBusRenewLeaseAction] = renewAction;
                    };
                });

            return new AsbOptions(azureServiceBusMessageQueue);
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