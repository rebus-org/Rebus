using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus2.Messages;
using Rebus2.Pipeline.Send;
using Rebus2.Transport;

namespace Rebus2.Pipeline
{
    public interface IStep { }

    public interface IIncomingStep : IStep
    {
        Task Process(IncomingStepContext context, Func<Task> next);
    }

    public interface IOutgoingStep : IStep
    {
        Task Process(OutgoingStepContext context, Func<Task> next);
    }

    public class IncomingStepContext : StepContext
    {
        public IncomingStepContext(TransportMessage message, ITransactionContext transactionContext)
        {
            Save(message);
            Save(transactionContext);
        }
    }

    public class OutgoingStepContext : StepContext
    {
        public OutgoingStepContext(Message logicalMessage, IEnumerable<string> destinationAddresses, ITransactionContext transactionContext)
        {
            Save(logicalMessage);
            Save(new DestinationAddresses(destinationAddresses));
            Save(transactionContext);
        }
    }

    public abstract class StepContext
    {
        public const string StepContextKey = "stepContext";

        readonly Dictionary<string, object> _items = new Dictionary<string, object>();

        //protected StepContext(TransportMessage receivedTransportMessage, ITransactionContext transactionContext)
        //{
        //    Save(receivedTransportMessage);
        //    Save(transactionContext);
        //}

        //protected StepContext(Message outgoingLogicalMessage)
        //{
        //    Save(outgoingLogicalMessage);
        //}

        public T Save<T>(T instance)
        {
            return Save(typeof(T).FullName, instance);
        }

        public T Save<T>(string key, T instance)
        {
            _items[key] = instance;
            return instance;
        }

        public T Load<T>()
        {
            return Load<T>(typeof(T).FullName);
        }

        public T Load<T>(string key)
        {
            object instance;
            return _items.TryGetValue(key, out instance)
                ? (T)instance
                : default(T);
        }
    }

}