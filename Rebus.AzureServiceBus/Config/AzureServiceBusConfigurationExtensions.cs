using System.Configuration;
using Rebus.AzureServiceBus;
using Rebus.AzureServiceBus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Subscriptions;
using Rebus.Threading;
using Rebus.Timeouts;
using Rebus.Transport;

// ReSharper disable once CheckNamespace
namespace Rebus.Config
{
    /// <summary>
    /// Configuration extensions for the Azure Service Bus transport
    /// </summary>
    public static class AzureServiceBusConfigurationExtensions
    {
        const string AsbSubStorageText = "The Azure Service Bus transport was inserted as the subscriptions storage because it has native support for pub/sub messaging";
        const string AsbTimeoutManagerText = "A disabled timeout manager was installed as part of the Azure Service Bus configuration, becuase the transport has native support for deferred messages";

        /// <summary>
        /// Configures Rebus to use Azure Service Bus to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static void UseAzureServiceBusAsOneWayClient(this StandardConfigurer<ITransport> configurer, string connectionStringNameOrConnectionString, AzureServiceBusMode mode = AzureServiceBusMode.Standard)
        {
            var connectionString = GetConnectionString(connectionStringNameOrConnectionString);

            if (mode == AzureServiceBusMode.Basic)
            {
                configurer.Register(c =>
                {
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                    return new BasicAzureServiceBusTransport(connectionString, null, rebusLoggerFactory, asyncTaskFactory);
                });
                OneWayClientBackdoor.ConfigureOneWayClient(configurer);
                return;
            }

            configurer
                .OtherService<AzureServiceBusTransport>()
                .Register(c =>
                {
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                    return new AzureServiceBusTransport(connectionString, null, rebusLoggerFactory, asyncTaskFactory);
                });

            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(c => c.Get<AzureServiceBusTransport>(), description: AsbSubStorageText);

            configurer.Register(c => c.Get<AzureServiceBusTransport>());

            configurer.OtherService<ITimeoutManager>().Register(c => new DisabledTimeoutManager(), description: AsbTimeoutManagerText);

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }
        /// <summary>
        /// Configures Rebus to use Azure Service Bus to transport messages as a one-way receiving client (i.e. will not be able to send any messages)
        /// (This enables the option to use a SAS key which only has read rights on the queue and no rights on the namespace itself)
        /// </summary>
        public static AzureServiceBusTransportSettings UseAzureServiceBusAsReadOnly(
            this StandardConfigurer<ITransport> configurer, string connectionStringNameOrConnectionString,
            string inputQueueAddress)
        {
            var connectionString = GetConnectionString(connectionStringNameOrConnectionString);
            var settingsBuilder = new AzureServiceBusTransportSettings();

            configurer.Register(c =>
                  {
                      var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                      var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                      var transport = new BasicReadOnlyAzureServiceBusTransport(connectionString, inputQueueAddress, rebusLoggerFactory, asyncTaskFactory);

                      if (settingsBuilder.PrefetchingEnabled)
                      {
                          transport.PrefetchMessages(settingsBuilder.NumberOfMessagesToPrefetch);
                      }

                      transport.AutomaticallyRenewPeekLock = settingsBuilder.AutomaticPeekLockRenewalEnabled;

                      transport.PartitioningEnabled = settingsBuilder.PartitioningEnabled;

                      return transport;
                  });

            return settingsBuilder;

        }


        /// <summary>
        /// Configures Rebus to use Azure Service Bus queues to transport messages, connecting to the service bus instance pointed to by the connection string
        /// (or the connection string with the specified name from the current app.config)
        /// </summary>
        public static AzureServiceBusTransportSettings UseAzureServiceBus(this StandardConfigurer<ITransport> configurer, string connectionStringNameOrConnectionString, string inputQueueAddress, AzureServiceBusMode mode = AzureServiceBusMode.Standard)
        {
            var connectionString = GetConnectionString(connectionStringNameOrConnectionString);
            var settingsBuilder = new AzureServiceBusTransportSettings();

            if (mode == AzureServiceBusMode.Basic)
            {
                configurer.Register(c =>
                {
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                    var transport = new BasicAzureServiceBusTransport(connectionString, inputQueueAddress, rebusLoggerFactory, asyncTaskFactory);

                    if (settingsBuilder.PrefetchingEnabled)
                    {
                        transport.PrefetchMessages(settingsBuilder.NumberOfMessagesToPrefetch);
                    }

                    transport.AutomaticallyRenewPeekLock = settingsBuilder.AutomaticPeekLockRenewalEnabled;

                    transport.PartitioningEnabled = settingsBuilder.PartitioningEnabled;

                    return transport;
                });

                return settingsBuilder;
            }

            configurer
                .OtherService<AzureServiceBusTransport>()
                .Register(c =>
                {
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                    var transport = new AzureServiceBusTransport(connectionString, inputQueueAddress, rebusLoggerFactory, asyncTaskFactory);

                    if (settingsBuilder.PrefetchingEnabled)
                    {
                        transport.PrefetchMessages(settingsBuilder.NumberOfMessagesToPrefetch);
                    }

                    transport.AutomaticallyRenewPeekLock = settingsBuilder.AutomaticPeekLockRenewalEnabled;

                    transport.PartitioningEnabled = settingsBuilder.PartitioningEnabled;

                    return transport;
                });

            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(c => c.Get<AzureServiceBusTransport>(), description: AsbSubStorageText);

            configurer.Register(c => c.Get<AzureServiceBusTransport>());

            configurer.OtherService<IPipeline>().Decorate(c =>
            {
                var pipeline = c.Get<IPipeline>();

                return new PipelineStepRemover(pipeline)
                    .RemoveIncomingStep(s => s.GetType() == typeof(HandleDeferredMessagesStep));
            });

            configurer.OtherService<ITimeoutManager>().Register(c => new DisabledTimeoutManager(), description: AsbTimeoutManagerText);

            return settingsBuilder;
        }

        static string GetConnectionString(string connectionStringNameOrConnectionString)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringNameOrConnectionString];

            return connectionStringSettings?.ConnectionString ?? connectionStringNameOrConnectionString;
        }
    }
}