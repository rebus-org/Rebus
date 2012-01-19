using System;

namespace Rebus.Messages
{
    public class RequestTimeoutMessage
    {
        public TimeSpan Timeout { get; set; }

        public string CorrelationId { get; set; }
    }

    public class TimeoutExpiredMessage
    {
        public DateTime DueTime { get; set; }

        public string CorrelationId { get; set; }
    }
}