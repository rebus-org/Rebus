using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Exceptions;
using Rebus.Messages;

namespace Rebus.Sagas
{
    public abstract class IdempotentSaga<TSagaData> : Saga<TSagaData> where TSagaData : IIdempotentSagaData, new()
    {
    }

    public interface IIdempotentSagaData : ISagaData
    {
        IdempotencyData IdempotencyData { get; }
    }

    public class IdempotencyData
    {
        readonly List<OutgoingMessages> _outgoingMessages = new List<OutgoingMessages>();

        public List<OutgoingMessages> OutgoingMessages
        {
            get { return _outgoingMessages; }
        }

        public bool HasAlreadyHandled(string messageId)
        {
            return _outgoingMessages.Any(o => o.MessageId == messageId);
        }

        public IEnumerable<OutgoingMessage> GetOutgoingMessages(string messageId)
        {
            try
            {
                return _outgoingMessages.First(o => o.MessageId == messageId).MessagesToSend;
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, "Could not get outgoing messages for message with ID {0}", messageId);
            }
        }

        public void MarkMessageAsHandled(string messageId)
        {
            GetOrCreate(messageId);
        }

        public void StoreOutgoingMessage(string messageId, IEnumerable<string> destinationAddresses, TransportMessage transportMessage)
        {
            GetOrCreate(messageId).MessagesToSend.Add(new OutgoingMessage
            {
                DestinationAddresses = destinationAddresses,
                TransportMessage = transportMessage
            });
        }

        OutgoingMessages GetOrCreate(string messageId)
        {
            var outgoingMessages = _outgoingMessages.FirstOrDefault(o => o.MessageId == messageId);

            if (outgoingMessages == null)
            {
                outgoingMessages = new OutgoingMessages(messageId, new List<OutgoingMessage>());
                _outgoingMessages.Add(outgoingMessages);
            }

            return outgoingMessages;
        }
    }

    public class OutgoingMessages
    {
        public OutgoingMessages(string messageId, IEnumerable<OutgoingMessage> messagesToSend)
        {
            MessageId = messageId;
            MessagesToSend = messagesToSend.ToList();
        }

        public string MessageId { get; private set; }
        public List<OutgoingMessage> MessagesToSend { get; private set; }
    }

    public class OutgoingMessage
    {
        public IEnumerable<string> DestinationAddresses { get; set; }
        public TransportMessage TransportMessage     { get; set; }
    }
}