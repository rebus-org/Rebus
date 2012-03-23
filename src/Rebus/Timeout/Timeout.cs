using System;

namespace Rebus.Timeout
{
    public class Timeout
    {
        public string ReplyTo { get; set; }
        public string CorrelationId { get; set; }
        public DateTime TimeToReturn { get; set; }
        public Guid SagaId { get; set; }
        public string CustomData { get; set; }

        public override string ToString()
        {
            return string.Format("{0}: {1} -> {2}", TimeToReturn, CorrelationId, ReplyTo);
        }
    }
}