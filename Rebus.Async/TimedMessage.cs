using System;
using Rebus.Messages;

namespace Rebus.Async
{
    class TimedMessage
    {
        public TimedMessage(Message message)
        {
            Message = message;
            Time = DateTime.UtcNow;
        }

        public Message Message { get; }
        public DateTime Time { get; }
        public TimeSpan Age => DateTime.UtcNow - Time;
    }
}