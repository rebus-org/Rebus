using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using System.Linq;

namespace Rebus.Testing
{
    /// <summary>
    /// Saga fixture that can help unit testing sagas.
    /// </summary>
    public class SagaFixture<T> where T : class, ISagaData, new()
    {
        readonly Saga<T> saga;
        readonly IList<T> availableSagaData;
        readonly List<T> deletedSagaData = new List<T>();
        readonly Dispatcher dispatcher;
        object currentLogicalMessage;

        /// <summary>
        /// Constructs the fixture with the given saga and the given saga data available. The <see cref="availableSagaData"/>
        /// list will be used to look for existing saga data instances, and new ones will be added to this list as well.
        /// </summary>
        [DebuggerStepThrough]
        public SagaFixture(Saga<T> saga, IList<T> availableSagaData)
        {
            var persister = new SagaFixtureSagaPersister<T>(availableSagaData, deletedSagaData);

            persister.CreatedNew += RaiseCreatedNewSagaData;
            persister.Correlated += RaiseCorrelatedWithExistingSagaData;
            persister.CouldNotCorrelate += RaiseCouldNotCorrelate;

            dispatcher = new Dispatcher(persister,
                                        new SagaFixtureHandlerActivator(saga), new InMemorySubscriptionStorage(),
                                        new TrivialPipelineInspector(), null);

            this.saga = saga;
            this.availableSagaData = availableSagaData;
        }

        void RaiseCouldNotCorrelate()
        {
            if (CouldNotCorrelate != null)
                CouldNotCorrelate(currentLogicalMessage);
        }

        void RaiseCorrelatedWithExistingSagaData(ISagaData d)
        {
            if (CorrelatedWithExistingSagaData != null)
                CorrelatedWithExistingSagaData(currentLogicalMessage, (T)d);
        }

        void RaiseCreatedNewSagaData(ISagaData d)
        {
            if (CreatedNewSagaData != null)
                CreatedNewSagaData(currentLogicalMessage, (T)d);
        }

        class SagaFixtureHandlerActivator : IActivateHandlers
        {
            readonly Saga sagaInstance;

            public SagaFixtureHandlerActivator(Saga sagaInstance)
            {
                this.sagaInstance = sagaInstance;
            }

            public IEnumerable<IHandleMessages<TMessage>> GetHandlerInstancesFor<TMessage>()
            {
                if (sagaInstance is IHandleMessages<TMessage>)
                    return new[] { (IHandleMessages<TMessage>)sagaInstance };

                return new IHandleMessages<TMessage>[0];
            }

            public void Release(IEnumerable handlerInstances)
            {
            }
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
        public void Handle<TMessage>(TMessage message)
        {
            currentLogicalMessage = message;

            try
            {
                dispatcher.GetType()
                    .GetMethod("Dispatch").MakeGenericMethod(message.GetType())
                    .Invoke(dispatcher, new object[] { message });
            }
            catch (TargetInvocationException tie)
            {
                var exception = (Exception)tie;

                if (exception.InnerException is TargetInvocationException)
                    exception = exception.InnerException;

                if (exception.InnerException is TargetInvocationException)
                    exception = exception.InnerException;

                var exceptionToRethrow = exception.InnerException;
                exceptionToRethrow.PreserveStackTrace();
                throw exceptionToRethrow;
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

        public class SagaFixtureSagaPersister<T> : IStoreSagaData where T : ISagaData
        {
            readonly IList<T> availableSagaData;
            readonly IList<T> deletedSagaData;
            readonly InMemorySagaPersister innerPersister;

            public SagaFixtureSagaPersister(IList<T> availableSagaData, IList<T> deletedSagaData)
            {
                innerPersister = new InMemorySagaPersister(availableSagaData.Cast<ISagaData>());

                this.availableSagaData = availableSagaData;
                this.deletedSagaData = deletedSagaData;
            }

            public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                innerPersister.Insert(sagaData, sagaDataPropertyPathsToIndex);
                availableSagaData.Add((T)sagaData);
                CreatedNew(sagaData);
            }

            public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                innerPersister.Update(sagaData, sagaDataPropertyPathsToIndex);
            }

            public void Delete(ISagaData sagaData)
            {
                innerPersister.Delete(sagaData);
                deletedSagaData.Add((T)sagaData);
            }

            public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
            {
                var result = innerPersister.Find<T>(sagaDataPropertyPath, fieldFromMessage);
                if (result != null)
                {
                    Correlated(result);
                }
                else
                {
                    CouldNotCorrelate();
                }
                return result;
            }

            public event Action<ISagaData> CreatedNew = delegate { };
            public event Action<ISagaData> Correlated = delegate { };
            public event Action CouldNotCorrelate = delegate { };
        }
    }
}