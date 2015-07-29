using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using System.Linq;

namespace Rebus.Testing
{
    /// <summary>
    /// Saga fixture that can help unit testing sagas.
    /// </summary>
    public class SagaFixture<TSagaData> : IEnumerable<TSagaData> where TSagaData : class, ISagaData, new()
    {
        readonly List<TSagaData> deletedSagaData = new List<TSagaData>();
        readonly Dispatcher dispatcher;
        readonly SagaFixtureSagaPersister<TSagaData> persister;

        object currentLogicalMessage;
        TSagaData latestSagaData;

        /// <summary>
        /// Constructs the fixture with the given saga handler and the given saga data initially available. Saga data
        /// instances are cloned.
        /// </summary>
        [DebuggerStepThrough]
        public SagaFixture(Saga<TSagaData> saga)
            : this(new[] { saga })
        {
        }

        /// <summary>
        /// Constructs the fixture with the given saga handlers and the given saga data initially available. Saga data
        /// instances are cloned.
        /// </summary>
        public SagaFixture(IEnumerable<Saga<TSagaData>> sagaInstances)
        {
            persister = new SagaFixtureSagaPersister<TSagaData>(deletedSagaData);

            persister.CreatedNew += RaiseCreatedNewSagaData;
            persister.Correlated += RaiseCorrelatedWithExistingSagaData;
            persister.CouldNotCorrelate += RaiseCouldNotCorrelate;

            var handlerActivator = new SagaFixtureHandlerActivator(sagaInstances);
            handlerActivator.MarkedAsComplete += () => RaiseMarkedAsComplete(persister.MostRecentlyLoadedSagaData);

            dispatcher = new Dispatcher(persister,
                                        handlerActivator, new InMemorySubscriptionStorage(),
                                        new TrivialPipelineInspector(), null, null);
        }

        void RaiseMarkedAsComplete(ISagaData sagaData)
        {
            if (MarkedAsComplete != null)
            {
                MarkedAsComplete(currentLogicalMessage, (TSagaData)sagaData);
            }
        }

        void RaiseCouldNotCorrelate()
        {
            if (CouldNotCorrelate != null)
            {
                CouldNotCorrelate(currentLogicalMessage);
            }
        }

        void RaiseCorrelatedWithExistingSagaData(ISagaData sagaData)
        {
            latestSagaData = (TSagaData)sagaData;

            if (CorrelatedWithExistingSagaData != null)
            {
                CorrelatedWithExistingSagaData(currentLogicalMessage, (TSagaData)sagaData);
            }
        }

        void RaiseCreatedNewSagaData(ISagaData sagaData)
        {
            latestSagaData = (TSagaData)sagaData;

            if (CreatedNewSagaData != null)
            {
                CreatedNewSagaData(currentLogicalMessage, (TSagaData)sagaData);
            }
        }

        class SagaFixtureHandlerActivator : IActivateHandlers
        {
            public event Action MarkedAsComplete = delegate { };

            readonly IEnumerable<Saga> sagaInstance;

            public SagaFixtureHandlerActivator(IEnumerable<Saga<TSagaData>> sagaInstance)
            {
                this.sagaInstance = sagaInstance;
            }

            public IEnumerable<IHandleMessages> GetHandlerInstancesFor<TMessage>()
            {
                return sagaInstance
                    .OfType<IHandleMessages<TMessage>>()
                    .Cast<IHandleMessages>()
                    .Concat(sagaInstance.OfType<IHandleMessagesAsync<TMessage>>());
            }

            public void Release(IEnumerable handlerInstances)
            {
                if (!handlerInstances.OfType<Saga>().Any(saga => saga.Complete)) return;

                MarkedAsComplete();
            }
        }

        /// <summary>
        /// Gets a list of all the saga data that is currently persisted
        /// </summary>
        public IEnumerable<TSagaData> AvailableSagaData
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
        /// Delegate type that can be used to detect when an incoming message gives rise to a saga data being marked as complete
        /// </summary>
        public delegate void MarkedAsCompleteEventHandler<in T>(object message, T sagaData);

        /// <summary>
        /// Delegate type that can be used to detect when a persistent instance of saga data has been deleted
        /// </summary>
        public delegate void DeletedEventHandler<in T>(object message, T sagaData);

        /// <summary>
        /// Delegate type that can be used to define events of when handling an incoming message results in an exception
        /// </summary>
        public delegate void ExceptionEventHandler(object message, Exception exception);

        /// <summary>
        /// Delegate type that can be used to define events of when an incoming message could have been handled by a saga handler but
        /// could not be correlated with an existing piece of saga data and it wasn't allowed to initiate a new saga
        /// </summary>
        public delegate void CouldNotCorrelateEventHandler(object message);

        /// <summary>
        /// Raised when the handling of an incoming message gives rise to an exception. When you add a listener to this event,
        /// the exception will be considered "handled" - i.e. it will not be re-thrown by the saga fixture.
        /// </summary>
        public event ExceptionEventHandler Exception;

        /// <summary>
        /// Raised during message dispatch when the message could be correlated with an existing saga data instance.
        /// The event is raised before the message is handled by the saga.
        /// </summary>
        public event CorrelatedWithExistingSagaDataEventHandler<TSagaData> CorrelatedWithExistingSagaData;

        /// <summary>
        /// Raised during message dispatch when the message could not be correlated with an existing saga data instance
        /// and a new saga data instance was created. The event is raised before the message is handled by the saga.
        /// </summary>
        public event CreatedNewSagaDataEventHandler<TSagaData> CreatedNewSagaData;

        /// <summary>
        /// Raised during message dispatch when the message could not be correlated with a saga data instance, and 
        /// creating a new saga data instance was not allowed.
        /// </summary>
        public event CouldNotCorrelateEventHandler CouldNotCorrelate;

        /// <summary>
        /// Raised when the message gives rise to the saga being marked as complete.
        /// </summary>
        public event MarkedAsCompleteEventHandler<TSagaData> MarkedAsComplete;

        /// <summary>
        /// Dispatches a message to the saga, raising the appropriate events along the way.
        /// </summary>
        public void Handle<TMessage>(TMessage message)
        {
            currentLogicalMessage = message;

            try
            {
                var task = (Task) dispatcher.GetType()
                    .GetMethod("Dispatch").MakeGenericMethod(message.GetType())
                    .Invoke(dispatcher, new object[] {message});

                task.Wait();
            }
            catch (AggregateException aggregateException)
            {
                Exception exception = aggregateException;

                if (exception.InnerException is TargetInvocationException)
                    exception = exception.InnerException;

                if (exception.InnerException is TargetInvocationException)
                    exception = exception.InnerException;

                var exceptionToRethrow = exception.InnerException;

                exceptionToRethrow.PreserveStackTrace();

                if (Exception != null)
                {
                    Exception(message, exceptionToRethrow);
                }
                else
                {
                    throw exceptionToRethrow;
                }
            }
        }

        /// <summary>
        /// Adds the given saga data to the underlying persister. Please note that the usual uniqueness constraint cannot be enforced
        /// when adding saga data this way, simply because it is impossible to know at this point which properties are correlation
        /// properties.
        /// </summary>
        public void AddSagaData(TSagaData sagaData)
        {
            persister.AddSagaData(sagaData);
        }

        /// <summary>
        /// Adds the saga data from the given sequence to the underlying persister. Please note that the usual uniqueness constraint cannot be enforced
        /// when adding saga data this way, simply because it is impossible to know at this point which properties are correlation
        /// properties.
        /// </summary>
        public void AddSagaData(IEnumerable<TSagaData> sagaData)
        {
            foreach (var data in sagaData)
            {
                persister.AddSagaData(data);
            }
        }

        /// <summary>
        /// Adds the saga data from the given sequence to the underlying persister. Please note that the usual uniqueness constraint cannot be enforced
        /// when adding saga data this way, simply because it is impossible to know at this point which properties are correlation
        /// properties.
        /// </summary>
        public void AddSagaData(params TSagaData[] sagaData)
        {
            foreach (var data in sagaData)
            {
                persister.AddSagaData(data);
            }
        }

        /// <summary>
        /// Gives access to the currently correlated piece of saga data. If none could be correlated, 
        /// null is returned.
        /// </summary>
        public TSagaData Data
        {
            get { return latestSagaData; }
        }

        class SagaFixtureSagaPersister<TSagaDataToStore> : IStoreSagaData where TSagaDataToStore : ISagaData
        {
            readonly IList<TSagaDataToStore> deletedSagaData;
            readonly InMemorySagaPersister innerPersister;

            public SagaFixtureSagaPersister(IList<TSagaDataToStore> deletedSagaData)
            {
                innerPersister = new InMemorySagaPersister();

                this.deletedSagaData = deletedSagaData;
            }

            public IEnumerable<TSagaData> AvailableSagaData
            {
                get { return innerPersister.Cast<TSagaData>(); }
            }

            public ISagaData MostRecentlyLoadedSagaData { get; set; }

            public void AddSagaData(TSagaData sagaData)
            {
                innerPersister.AddSagaData(sagaData);
            }

            public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                innerPersister.Insert(sagaData, sagaDataPropertyPathsToIndex);

                if (CreatedNew != null)
                {
                    CreatedNew(sagaData);
                }
            }

            public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                innerPersister.Update(sagaData, sagaDataPropertyPathsToIndex);
            }

            public void Delete(ISagaData sagaData)
            {
                innerPersister.Delete(sagaData);
                deletedSagaData.Add((TSagaDataToStore)sagaData);

                if (Deleted != null)
                {
                    Deleted(sagaData);
                }
            }

            public TSagaDataToFind Find<TSagaDataToFind>(string sagaDataPropertyPath, object fieldFromMessage) where TSagaDataToFind : class, ISagaData
            {
                var result = innerPersister.Find<TSagaDataToFind>(sagaDataPropertyPath, fieldFromMessage);

                if (result != null)
                {
                    MostRecentlyLoadedSagaData = result;

                    if (Correlated != null)
                    {
                        Correlated(result);
                    }
                }
                else
                {
                    MostRecentlyLoadedSagaData = null;

                    if (CouldNotCorrelate != null)
                    {
                        CouldNotCorrelate();
                    }
                }
                return result;
            }

            public event Action<ISagaData> CreatedNew;

            public event Action<ISagaData> Correlated;

            public event Action<ISagaData> Deleted;

            public event Action CouldNotCorrelate;
        }

        /// <summary>
        /// Adds the given saga data to the underlying persister. Please note that the usual uniqueness constraint cannot be enforced
        /// when adding saga data this way, simply because it is impossible to know at this point which properties are correlation
        /// properties.
        /// </summary>
        public void Add(TSagaData someSagaData)
        {
            AddSagaData(someSagaData);
        }

        /// <summary>
        /// Enumerates all persistent saga data
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return AvailableSagaData.GetEnumerator();
        }

        IEnumerator<TSagaData> IEnumerable<TSagaData>.GetEnumerator()
        {
            return AvailableSagaData.GetEnumerator();
        }
    }
}
