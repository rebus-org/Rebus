using System;

namespace Rebus.Config
{
    /// <summary>
    /// Allows for configuring additional options for the Azure Service Bus transport
    /// </summary>
    public class AzureServiceBusTransportSettings
    {
        internal bool PrefetchingEnabled { get; set; }
        internal int NumberOfMessagesToPrefetch { get; set; }
        internal bool PartitioningEnabled { get; set; }
        internal bool DoNotCreateQueuesEnabled { get; set; }
        /// <summary>
        /// Enables partitioning whereby Azure Service Bus will be able to distribute messages between message stores
        /// and this way increase throughput. Enabling partitioning only has an effect on newly created queues.
        /// </summary>
        public AzureServiceBusTransportSettings EnablePartitioning()
        {
            PartitioningEnabled = true;

            return this;
        }

        /// <summary>
        /// Enables prefetching whereby a batch of messages will be prefetched instead of only one at a time.
        /// By enabling prefetching, the automatic peek lock renewal will be disabled, because it is assumed
        /// that prefetching will be enabled only in cases where messages can be processed fairly quickly.
        /// </summary>
        public AzureServiceBusTransportSettings EnablePrefetching(int numberOfMessagesToPrefetch)
        {
            if (numberOfMessagesToPrefetch < 1)
            {
                throw new ArgumentOutOfRangeException(string.Format("Cannot set prefetching to {0} messages - must be at least 1", numberOfMessagesToPrefetch));
            }

            PrefetchingEnabled = true;
            NumberOfMessagesToPrefetch = numberOfMessagesToPrefetch;
            
            return this;
        }

        internal bool AutomaticPeekLockRenewalEnabled { get; set; }

        /// <summary>
        /// Enables automatic peek lock renewal. Only enable this if you intend on handling messages for a long long time, and
        /// DON'T intend on handling messages quickly - it will have an impact on message receive, so only enable it if you
        /// need it. You should usually strive after keeping message processing times low, much lower than the 5 minute lease
        /// you get with Azure Service Bus.
        /// </summary>
        public AzureServiceBusTransportSettings AutomaticallyRenewPeekLock()
        {
            AutomaticPeekLockRenewalEnabled = true;

            return this;
        }

        public AzureServiceBusTransportSettings DoNotCreateQueues()
        {
            DoNotCreateQueuesEnabled = true;
            return this;
        }
    }
}