using System;

namespace Rebus.EventStore
{
    internal class EventStoreQueue
    {
        public readonly string StreamId;

        public EventStoreQueue(string queueId)
        {
            if (queueId == null) throw new ArgumentNullException("queueId");
            if(queueId.Contains("-")) throw new ArgumentException("queueId cannot contain '-' since that would probably create a stream category.");

            StreamId = queueId;
        }
    }
}
