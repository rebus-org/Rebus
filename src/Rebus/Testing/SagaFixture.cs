using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Ponder;

namespace Rebus.Testing
{
    /// <summary>
    /// Saga fixture that can help unit testing sagas.
    /// </summary>
    public class SagaFixture<T> where T : class, ISagaData, new()
    {
        readonly Saga<T> saga;
        readonly IList<T> availableSagaData;
        readonly ConcurrentDictionary<Type, Correlation> correlations;

        /// <summary>
        /// Constructs the fixture with the given saga and the given saga data available. The <see cref="availableSagaData"/>
        /// list will be used to look for existing saga data instances, and new ones will be added to this list as well.
        /// </summary>
        public SagaFixture(Saga<T> saga, IList<T> availableSagaData)
        {
            this.saga = saga;
            this.availableSagaData = availableSagaData;
            saga.ConfigureHowToFindSaga();
            correlations = saga.Correlations;
        }

        public delegate void CorrelatedWithExistingSagaDataEventHandler<T>(object message, T sagaData);
        
        public delegate void CreatedNewSagaDataEventHandler<T>(object message, T sagaData);
        
        public delegate void CouldNotCorrelateEventHandler(object message);

        /// <summary>
        /// Gets raised during message dispatch when the message could be correlated with an existing saga data instance.
        /// The event is raised before the message is handled by the saga.
        /// </summary>
        public event CorrelatedWithExistingSagaDataEventHandler<T> CorrelatedWithExistingSagaData = delegate { };

        /// <summary>
        /// Gets raised during message dispatch when the message could not be correlated with an existing saga data instance
        /// and a new saga data instance was created. The event is raised before the message is handled by the saga.
        /// </summary>
        public event CreatedNewSagaDataEventHandler<T> CreatedNewSagaData = delegate { };

        /// <summary>
        /// Gets raised during message dispatch when the message could not be correlated with a saga data instance, and 
        /// creating a new saga data instance was not allowed.
        /// </summary>
        public event CouldNotCorrelateEventHandler CouldNotCorrelate = delegate { };

        /// <summary>
        /// Dispatches a message to the saga, raising the appropriate events along the way.
        /// </summary>
        public void Handle<TMessage>(TMessage message)
        {
            var existingSagaData = availableSagaData.SingleOrDefault(data => Correlates(correlations, message, data));

            if (existingSagaData != null)
            {
                saga.Data = existingSagaData;
                saga.IsNew = false;
                CorrelatedWithExistingSagaData(message, saga.Data);
                Dispatch(message);
                return;
            }

            if (saga.GetType().GetInterfaces().Contains(typeof(IAmInitiatedBy<TMessage>)))
            {
                saga.Data = new T();
                saga.IsNew = true;
                availableSagaData.Add(saga.Data);
                CreatedNewSagaData(message, saga.Data);
                Dispatch(message);
                return;
            }

            CouldNotCorrelate(message);
        }

        /// <summary>
        /// Gives access to the currently correlated piece of saga data. If none could be correlated, 
        /// null is returned.
        /// </summary>
        public T Data
        {
            get { return saga.Data; }
        }

        bool Correlates(ConcurrentDictionary<Type, Correlation> concurrentDictionary, object message, T data)
        {
            var messageType = message.GetType();
            if (!concurrentDictionary.ContainsKey(messageType)) return false;

            var correlation = concurrentDictionary[messageType];

            var path = correlation.SagaDataPropertyPath;

            var fieldFromMessage = (correlation.FieldFromMessage(message) ?? "").ToString();
            var fieldFromSagaData = (Reflect.Value(data, path) ?? "").ToString();

            return fieldFromMessage == fieldFromSagaData;
        }

        void Dispatch<TMessage>(TMessage message)
        {
            saga.GetType().GetMethod("Handle", new[] { typeof(TMessage) })
                .Invoke(saga, new object[] { message });
        }
    }
}