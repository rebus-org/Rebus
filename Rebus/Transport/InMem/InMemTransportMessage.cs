using System;
using System.Collections.Generic;
using Rebus.Messages;

namespace Rebus.Transport.InMem
{
    public class InMemTransportMessage
    {
        readonly DateTime _creationTime = DateTime.UtcNow;

        public InMemTransportMessage(TransportMessage transportMessage)
        {
            Headers = transportMessage.Headers;
            Body = transportMessage.Body;
        }

        public TimeSpan Age
        {
            get { return DateTime.UtcNow - _creationTime; }
        }

        public Dictionary<string,string> Headers { get; private set; }
            
        public byte[] Body { get; private set; }

        public TransportMessage ToTransportMessage()
        {
            return new TransportMessage(Headers, Body);
        }
    }
}