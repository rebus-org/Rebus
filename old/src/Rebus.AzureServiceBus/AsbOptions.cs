using System;

namespace Rebus.AzureServiceBus
{
    public interface IAsbOptions
    {
        IAsbOptions AutomaticallyRenewPeekLockEvery(TimeSpan customTimeSpan);
        IAsbOptions DoNotAutomaticallyRenewPeekLock();
    }

    public class AsbOptions : IAsbOptions
    {
        readonly AzureServiceBusMessageQueue queue;

        public AsbOptions(AzureServiceBusMessageQueue queue)
        {
            this.queue = queue;
        }

        public IAsbOptions AutomaticallyRenewPeekLockEvery(TimeSpan customTimeSpan)
        {
            queue.SetAutomaticPeekLockRenewalInterval(customTimeSpan);
            return this;
        }

        public IAsbOptions DoNotAutomaticallyRenewPeekLock()
        {
            queue.SetAutomaticPeekLockRenewalInterval(TimeSpan.FromSeconds(0));
            return this;
        }
    }
}