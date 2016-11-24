using System;
using System.Collections.Generic;
using System.Threading;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Sagas;
using Rebus.Testing.Internals;
using Rebus.Transport.InMem;

namespace Rebus.Testing
{
    /// <summary>
    /// Saga fixture factory class - can be used to create an appropriate <see cref="SagaFixture{TSagaHandler}"/> for a saga
    /// handler to participate in white-box testing scenarios
    /// </summary>
    public static class SagaFixture
    {
        internal static bool LoggingInfoHasBeenShown;

        /// <summary>
        /// Creates a saga fixture for the specified saga handler, which must have a default constructor. If the saga handler
        /// requires any parameters to be created, use the <see cref="For{TSagaHandler}(Func{TSagaHandler})"/> overload that
        /// accepts a factory function as a saga handler instance creator
        /// </summary>
        public static SagaFixture<TSagaHandler> For<TSagaHandler>() where TSagaHandler : Saga, IHandleMessages, new()
        {
            Func<TSagaHandler> factory = () =>
            {
                try
                {
                    return new TSagaHandler();
                }
                catch (Exception exception)
                {
                    throw new ArgumentException($"Could not create new saga handler instance of type {typeof(TSagaHandler)}", exception);
                }
            };

            return For(factory);
        }

        /// <summary>
        /// Creates a saga fixture for the specified saga handler, which will be instantiated by the given factory method
        /// </summary>
        public static SagaFixture<TSagaHandler> For<TSagaHandler>(Func<TSagaHandler> sagaHandlerFactory) where TSagaHandler : Saga, IHandleMessages
        {
            if (sagaHandlerFactory == null) throw new ArgumentNullException(nameof(sagaHandlerFactory));

            var activator = new BuiltinHandlerActivator();
            activator.Register(sagaHandlerFactory);

            if (!LoggingInfoHasBeenShown)
            {
                Console.WriteLine("Remember that the saga fixture collects all internal logs which you can access with fixture.LogEvents");
                LoggingInfoHasBeenShown = true;
            }

            return new SagaFixture<TSagaHandler>(activator);
        }
    }

    /// <summary>
    /// Saga fixture that wraps an in-mem Rebus that 
    /// </summary>
    public class SagaFixture<TSagaHandler> : IDisposable where TSagaHandler : Saga
    {
        const string SagaInputQueueName = "sagafixture";
        readonly BuiltinHandlerActivator _activator;
        readonly InMemNetwork _network;
        readonly InMemorySagaStorage _inMemorySagaStorage;
        readonly LockStepper _lockStepper;
        readonly TestLoggerFactory _loggerFactory;

        /// <summary>
        /// Event that is raised whenever a message could be successfully correlated with a saga data instance. The instance
        /// is passed to the event handler
        /// </summary>
        public event Action<ISagaData> Correlated;

        /// <summary>
        /// Event that is raised whenever a message could NOT be successfully correlated with a saga data instance. The event is
        /// raised regardless of whether the incoming message is allowed to initiate a new saga or not.
        /// </summary>
        public event Action CouldNotCorrelate;

        /// <summary>
        /// Event that is raised when the incoming message resulted in creating a new saga data instance. The created instance
        /// is passed to the event handler.
        /// </summary>
        public event Action<ISagaData> Created;

        /// <summary>
        /// Event that is raised when the incoming message resulted in updating an existing saga data instance. The updated instance
        /// is passed to the event handler.
        /// </summary>
        public event Action<ISagaData> Updated;

        /// <summary>
        /// Event that is raised when the incoming message resulted in deleting an existing saga data instance. The deleted instance
        /// is passed to the event handler.
        /// </summary>
        public event Action<ISagaData> Deleted;

        internal SagaFixture(BuiltinHandlerActivator activator)
        {
            if (activator == null) throw new ArgumentNullException(nameof(activator));
            _activator = activator;
            _network = new InMemNetwork();

            _inMemorySagaStorage = new InMemorySagaStorage();
            _inMemorySagaStorage.Correlated += sagaData => Correlated?.Invoke(sagaData);
            _inMemorySagaStorage.CouldNotCorrelate += () => CouldNotCorrelate?.Invoke();
            _inMemorySagaStorage.Created += sagaData => Created?.Invoke(sagaData);
            _inMemorySagaStorage.Updated += sagaData => Updated?.Invoke(sagaData);
            _inMemorySagaStorage.Deleted += sagaData => Deleted?.Invoke(sagaData);

            _lockStepper = new LockStepper();

            _loggerFactory = new TestLoggerFactory();

            Configure.With(activator)
                .Logging(l => l.Use(_loggerFactory))
                .Transport(t => t.UseInMemoryTransport(_network, SagaInputQueueName))
                .Sagas(s => s.Register(c => _inMemorySagaStorage))
                .Options(o =>
                {
                    o.SetNumberOfWorkers(1);
                    o.SetMaxParallelism(1);

                    o.Decorate<IPipeline>(c =>
                    {
                        var pipeline = c.Get<IPipeline>();

                        return new PipelineStepConcatenator(pipeline)
                            .OnReceive(_lockStepper, PipelineAbsolutePosition.Front);
                    });
                })
                .Start();
        }

        /// <summary>
        /// Gets all of the currently existing saga data instances
        /// </summary>
        public IEnumerable<ISagaData> Data => _inMemorySagaStorage.Instances;

        /// <summary>
        /// Gets all log events emitted by the internal Rebus instance
        /// </summary>
        public IEnumerable<LogEvent> LogEvents => _loggerFactory.LogEvents;

        /// <summary>
        /// Delivers the given message to the saga handler
        /// </summary>
        public void Deliver(object message, Dictionary<string, string> optionalHeaders = null, int deliveryTimeoutSeconds = 5)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var resetEvent = new ManualResetEvent(false);
            _lockStepper.AddResetEvent(resetEvent);

            _activator.Bus.SendLocal(message, optionalHeaders);

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(deliveryTimeoutSeconds)))
            {
                throw new TimeoutException($"Message {message} did not seem to have been processed withing {deliveryTimeoutSeconds} s timeout");
            }
        }

        /// <summary>
        /// Adds the given saga data to the available saga data in the saga fixture. If the saga data is not provided
        /// with an ID, a new guid will automatically be assigned internally.
        /// </summary>
        public void Add(ISagaData sagaDataInstance)
        {
            _inMemorySagaStorage.AddInstance(sagaDataInstance);
        }

        /// <summary>
        /// Adds the given saga data instances to the available saga data in the fixture. If the saga data instances have not been provided
        /// with an ID, a new guid will automatically be assigned internally.
        /// </summary>
        public void AddRange(IEnumerable<ISagaData> sagaDataInstances)
        {
            foreach (var sagaDataInstance in sagaDataInstances)
            {
                Add(sagaDataInstance);
            }
        }

        /// <summary>
        /// Shuts down the in-mem bus that holds the saga handler
        /// </summary>
        public void Dispose()
        {
            _activator.Dispose();
        }
    }
}