using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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
        readonly List<T> deletedSagaData = new List<T>();

        /// <summary>
        /// Constructs the fixture with the given saga and the given saga data available. The <see cref="availableSagaData"/>
        /// list will be used to look for existing saga data instances, and new ones will be added to this list as well.
        /// </summary>
        [DebuggerStepThrough]
        public SagaFixture(Saga<T> saga, IList<T> availableSagaData)
        {
            this.saga = saga;
            this.availableSagaData = availableSagaData;
            saga.ConfigureHowToFindSaga();
            correlations = saga.Correlations;
        }

        /// <summary>
        /// Constructs the fixture with the given saga.
        /// </summary>
        [DebuggerStepThrough]
        public SagaFixture(Saga<T> saga)
            : this(saga, new List<T>())
        {
        }

        public IList<T> AvailableSagaData
        {
            get { return availableSagaData; }
        }

        public IList<T> DeletedSagaData
        {
            get { return deletedSagaData; }
        }

        public delegate void CorrelatedWithExistingSagaDataEventHandler<in TSagaData>(object message, TSagaData sagaData);

        public delegate void CreatedNewSagaDataEventHandler<in TSagaData>(object message, TSagaData sagaData);

        public delegate void CouldNotCorrelateEventHandler(object message);

        /// <summary>
        /// Gets raised during message dispatch when the message could be correlated with an existing saga data instance.
        /// The event is raised before the message is handled by the saga.
        /// </summary>
        public event CorrelatedWithExistingSagaDataEventHandler<T> CorrelatedWithExistingSagaData;

        /// <summary>
        /// Gets raised during message dispatch when the message could not be correlated with an existing saga data instance
        /// and a new saga data instance was created. The event is raised before the message is handled by the saga.
        /// </summary>
        public event CreatedNewSagaDataEventHandler<T> CreatedNewSagaData;

        /// <summary>
        /// Gets raised during message dispatch when the message could not be correlated with a saga data instance, and 
        /// creating a new saga data instance was not allowed.
        /// </summary>
        public event CouldNotCorrelateEventHandler CouldNotCorrelate;

        /// <summary>
        /// Dispatches a message to the saga, raising the appropriate events along the way.
        /// </summary>
        [DebuggerStepThrough]
        public void Handle<TMessage>(TMessage message)
        {
            try
            {
                InnerHandle(message);
            }
            catch (TargetInvocationException tie)
            {
                var exceptionToThrow = tie.InnerException;
                exceptionToThrow.PreserveStackTrace();
                throw exceptionToThrow;
            }
        }

        /// <summary>
        /// Gives access to the currently correlated piece of saga data. If none could be correlated, 
        /// null is returned.
        /// </summary>
        public T Data
        {
            get { return saga.Data; }
        }

        [DebuggerStepThrough]
        void InnerHandle<TMessage>(TMessage message)
        {
            var existingSagaData = availableSagaData.SingleOrDefault(data => Correlates(correlations, message, data));

            if (existingSagaData != null)
            {
                saga.Data = existingSagaData;
                saga.IsNew = false;
                if (CorrelatedWithExistingSagaData != null) CorrelatedWithExistingSagaData(message, saga.Data);
                Dispatch(message);
                if (saga.Complete)
                {
                    availableSagaData.Remove(saga.Data);
                    deletedSagaData.Add(saga.Data);
                }
                return;
            }

            if (saga.GetType().GetInterfaces().Contains(typeof(IAmInitiatedBy<TMessage>)))
            {
                saga.Data = new T();
                saga.IsNew = true;
                if (CreatedNewSagaData != null) CreatedNewSagaData(message, saga.Data);
                Dispatch(message);
                if (!saga.Complete)
                {
                    availableSagaData.Add(saga.Data);
                }
                else
                {
                    deletedSagaData.Add(saga.Data);
                }
                return;
            }

            if (CouldNotCorrelate != null) CouldNotCorrelate(message);
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        void Dispatch<TMessage>(TMessage message)
        {
            saga.Complete = false;
            saga.GetType().GetMethod("Handle", new[] { typeof(TMessage) })
                .Invoke(saga, new object[] { message });
        }
    }
}