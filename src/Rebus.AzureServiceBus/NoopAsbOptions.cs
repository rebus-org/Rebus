using System;

namespace Rebus.AzureServiceBus.Queues
{
    public class NoopAsbOptions : IAsbOptions
    {
        public IAsbOptions AutomaticallyRenewPeekLockEvery(TimeSpan customTimeSpan)
        {
            return this;
        }

        public IAsbOptions DoNotAutomaticallyRenewPeekLock()
        {
            return this;
        }
    }
}