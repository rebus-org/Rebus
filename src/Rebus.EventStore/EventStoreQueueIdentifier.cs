using System;

namespace Rebus.EventStore
{
    internal class EventStoreQueueIdentifier
    {
        public readonly string StreamId;

        public EventStoreQueueIdentifier(string queueId)
        {
            if (queueId == null) throw new ArgumentNullException("queueId");
            if(queueId.Contains("-")) throw new ArgumentException("queueId cannot contain '-' since that would probably create a stream category.");

            StreamId = queueId;
        }
    }
}
