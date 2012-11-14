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
    public class SagaFixture<TSagaData> where TSagaData : class, ISagaData, new()
    {
        readonly Saga<TSagaData> saga;
        readonly List<TSagaData> deletedSagaData = new List<TSagaData>();
        readonly Dispatcher dispatcher;
        readonly SagaFixtureSagaPersister<TSagaData> persister;
        object currentLogicalMessage;

        /// <summary>
        /// Constructs the fixture with the given saga and the given saga data initially available. Saga data
        /// instances are cloned.
        /// </summary>
        [DebuggerStepThrough]
        public SagaFixture(Saga<TSagaData> saga, IEnumerable<TSagaData> availableSagaData)
        {
            persister = new SagaFixtureSagaPersister<TSagaData>(availableSagaData, deletedSagaData);

            persister.CreatedNew += RaiseCreatedNewSagaData;
            persister.Correlated += RaiseCorrelatedWithExistingSagaData;
            persister.CouldNotCorrelate += RaiseCouldNotCorrelate;

            dispatcher = new Dispatcher(persister,
                                        new SagaFixtureHandlerActivator(saga), new InMemorySubscriptionStorage(),
                                        new TrivialPipelineInspector(), null);

            this.saga = saga;
        }

        void RaiseCouldNotCorrelate()
        {
            if (CouldNotCorrelate != null)
                CouldNotCorrelate(currentLogicalMessage);
        }

        void RaiseCorrelatedWithExistingSagaData(ISagaData d)
        {
            if (CorrelatedWithExistingSagaData != null)
                CorrelatedWithExistingSagaData(currentLogicalMessage, (TSagaData)d);
        }

        void RaiseCreatedNewSagaData(ISagaData d)
        {
            if (CreatedNewSagaData != null)
                CreatedNewSagaData(currentLogicalMessage, (TSagaData)d);
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
        public SagaFixture(Saga<TSagaData> saga)
            : this(saga, new List<TSagaData>())
        {
        }

        /// <summary>
        /// Gets a list of all the saga data that is currently persisted
        /// </summary>
        public IList<TSagaData> AvailableSagaData
        {
            get { return persister.AvailableSagaData; }
        }

        /// <summary>
        /// Gets a list of all the saga data that has been marked as complete
        /// </summary>
        public IList<TSagaData> DeletedSagaData
        {
            get { return deletedSagaData; }
        }

        /// <summary>
        /// Delegate type that can be used to define events of when an incoming message can be correlated with an existing piece of saga data
        /// </summary>
        public delegate void CorrelatedWithExistingSagaDataEventHandler<in T>(object message, T sagaData);

        /// <summary>
        /// Delegate type that can be used to define events of when an incoming message gives rise to a new instance of saga data
        /// </summary>
        public delegate void CreatedNewSagaDataEventHandler<in T>(object message, T sagaData);

        /// <summary>
        /// Delegate type that can be used to define events of when an incoming message could have been handled by a saga handler but
        /// could not be correlated with an existing piece of saga data and it wasn't allowed to initiate a new saga
        /// </summary>
        public delegate void CouldNotCorrelateEventHandler(object message);

        /// <summary>
        /// Gets raised during message dispatch when the message could be correlated with an existing saga data instance.
        /// The event is raised before the message is handled by the saga.
        /// </summary>
        public event CorrelatedWithExistingSagaDataEventHandler<TSagaData> CorrelatedWithExistingSagaData;

        /// <summary>
        /// Gets raised during message dispatch when the message could not be correlated with an existing saga data instance
        /// and a new saga data instance was created. The event is raised before the message is handled by the saga.
        /// </summary>
        public event CreatedNewSagaDataEventHandler<TSagaData> CreatedNewSagaData;

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
        public TSagaData Data
        {
            get { return saga.Data; }
        }

        class SagaFixtureSagaPersister<TSagaDataToStore> : IStoreSagaData where TSagaDataToStore : ISagaData
        {
            readonly IList<TSagaDataToStore> deletedSagaData;
            readonly InMemorySagaPersister innerPersister;

            public SagaFixtureSagaPersister(IEnumerable<TSagaDataToStore> availableSagaData, IList<TSagaDataToStore> deletedSagaData)
            {
                innerPersister = new InMemorySagaPersister(availableSagaData.Cast<ISagaData>());

                this.deletedSagaData = deletedSagaData;
            }

            public IList<TSagaData> AvailableSagaData
            {
                get { return innerPersister.Cast<TSagaData>().ToList(); }
            }

            public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                innerPersister.Insert(sagaData, sagaDataPropertyPathsToIndex);
                CreatedNew(sagaData);
            }

            public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                innerPersister.Update(sagaData, sagaDataPropertyPathsToIndex);
            }

            public void Delete(ISagaData sagaData)
            {
                innerPersister.Delete(sagaData);
                deletedSagaData.Add((TSagaDataToStore)sagaData);
            }

            public TSagaDataToFind Find<TSagaDataToFind>(string sagaDataPropertyPath, object fieldFromMessage) where TSagaDataToFind : class, ISagaData
            {
                var result = innerPersister.Find<TSagaDataToFind>(sagaDataPropertyPath, fieldFromMessage);
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