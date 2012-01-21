using System;

namespace Rebus.Messages
{
    public class TimeoutReply : IRebusControlMessage
    {
        public DateTime DueTime { get; set; }

        public string CorrelationId { get; set; }
    }
}