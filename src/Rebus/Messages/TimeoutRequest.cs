using System;

namespace Rebus.Messages
{
    public class TimeoutRequest : IRebusControlMessage
    {
        public TimeSpan Timeout { get; set; }

        public string CorrelationId { get; set; }
    }
}