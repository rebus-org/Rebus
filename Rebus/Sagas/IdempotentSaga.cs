using System.Collections.Generic;
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
        public bool HasAlreadyHandled(string messageId)
        {
            return false;
        }

        public IEnumerable<OutgoingMessage> GetOutgoingMessages(string messageId)
        {
            return new List<OutgoingMessage>();
        }
    }

    public class OutgoingMessage
    {
        public string Destination { get; set; }
        public TransportMessage TransportMessage     { get; set; }
    }
}