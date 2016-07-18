using System;
using System.Configuration;
using System.Linq;
using Rebus.Activation;
using Rebus.Backoff;
using Rebus.Bus;
using Rebus.DataBus;
using Rebus.Handlers;
using Rebus.Injection;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Retry;
using Rebus.Retry.ErrorTracking;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Subscriptions;
using Rebus.Threading;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Timeouts;
using Rebus.Transport;
using Rebus.Workers;
using Rebus.Workers.ThreadBased;

namespace Rebus.Config
{
    /// <summary>
    /// Basic skeleton of the fluent configuration builder. Contains a method for each aspect that can be configured
    /// </summary>
    public class RebusConfigurer
    {
        readonly Injectionist _injectionist = new Injectionist();
        readonly Options _options = new Options();

        bool _hasBeenStarted;

        internal RebusConfigurer(IHandlerActivator handlerActivator)
        {
            _injectionist.Register(c => handlerActivator);

            if (handlerActivator is IContainerAdapter)
            {
                _injectionist.Register(c => (IContainerAdapter)handlerActivator);
            }
        }

        /// <summary>
        /// Configures how Rebus logs stuff that happens
        /// </summary>
        public RebusConfigurer Logging(Action<RebusLoggingConfigurer> configurer)
        {
            configurer(new RebusLoggingConfigurer(_injectionist));
            return this;
        }

        /// <summary>
        /// Configures how Rebus sends/receives messages by allowing for choosing which implementation of <see cref="ITransport"/> to use
        /// </summary>
        public RebusConfigurer Transport(Action<StandardConfigurer<ITransport>> configurer)
        {
            configurer(new StandardConfigurer<ITransport>(_injectionist, _options));
            return this;
        }

        /// <summary>
        /// Configures how Rebus routes messages by allowing for choosing which implementation of <see cref="IRouter"/> to use
        /// </summary>
        public RebusConfigurer Routing(Action<StandardConfigurer<IRouter>> configurer)
        {
            configurer(new StandardConfigurer<IRouter>(_injectionist, _options));
            return this;
        }

        /// <summary>
        /// Configures how Rebus persists saga data by allowing for choosing which implementation of <see cref="ISagaStorage"/> to use
        /// </summary>
        public RebusConfigurer Sagas(Action<StandardConfigurer<ISagaStorage>> configurer)
        {
            configurer(new StandardConfigurer<ISagaStorage>(_injectionist, _options));
            return this;
        }

        /// <summary>
        /// Configures how Rebus persists subscriptions by allowing for choosing which implementation of <see cref="ISubscriptionStorage"/> to use
        /// </summary>
        public RebusConfigurer Subscriptions(Action<StandardConfigurer<ISubscriptionStorage>> configurer)
        {
            configurer(new StandardConfigurer<ISubscriptionStorage>(_injectionist, _options));
            return this;
        }

        /// <summary>
        /// Configures how Rebus serializes messages by allowing for choosing which implementation of <see cref="ISerializer"/> to use
        /// </summary>
        public RebusConfigurer Serialization(Action<StandardConfigurer<ISerializer>> configurer)
        {
            configurer(new StandardConfigurer<ISerializer>(_injectionist, _options));
            return this;
        }

        /// <summary>
        /// Configures how Rebus defers messages to the future by allowing for choosing which implementation of <see cref="ITimeoutManager"/> to use
        /// </summary>
        public RebusConfigurer Timeouts(Action<StandardConfigurer<ITimeoutManager>> configurer)
        {
            configurer(new StandardConfigurer<ITimeoutManager>(_injectionist, _options));
            return this;
        }

        /// <summary>
        /// Configures additional options about how Rebus works
        /// </summary>
        public RebusConfigurer Options(Action<OptionsConfigurer> configurer)
        {
            configurer(new OptionsConfigurer(_options, _injectionist));
            return this;
        }

        /// <summary>
        /// Finishes the setup of the bus, using default implementations for the options that have not explicitly been set.
        /// The only requirement, is that you must call <see cref="Transport"/> and select which transport to use - everything
        /// else can run with a default option. It should be noted though, that several of the defaults (e.g. in-mem persistence
        /// options for saga storage, subscriptions, and timeouts) are not meant for production use, and should probably be
        /// replaced by something that is actually persistent.
        /// </summary>
        public IBus Start()
        {
            VerifyRequirements();

            PossiblyRegisterDefault<IRebusLoggerFactory>(c => new ConsoleLoggerFactory(true));

            //PossiblyRegisterDefault<IAsyncTaskFactory>(c => new TimerAsyncTaskFactory(c.Get<IRebusLoggerFactory>()));
            PossiblyRegisterDefault<IAsyncTaskFactory>(c => new TplAsyncTaskFactory(c.Get<IRebusLoggerFactory>()));

            PossiblyRegisterDefault<IRouter>(c =>
            {
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                return new TypeBasedRouter(rebusLoggerFactory);
            });

            PossiblyRegisterDefault<ISubscriptionStorage>(c => new InMemorySubscriptionStorage());

            PossiblyRegisterDefault<ISagaStorage>(c => new InMemorySagaStorage());

            PossiblyRegisterDefault<ISerializer>(c => new JsonSerializer());

            PossiblyRegisterDefault<IPipelineInvoker>(c => new DefaultPipelineInvoker());

            PossiblyRegisterDefault<IBackoffStrategy>(c => new SimpleConstantPollingBackoffStrategy());

            PossiblyRegisterDefault<IWorkerFactory>(c =>
            {
                var transport = c.Get<ITransport>();
                var pipeline = c.Get<IPipeline>();
                var pipelineInvoker = c.Get<IPipelineInvoker>();
                var backoffStrategy = c.Get<IBackoffStrategy>();
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                return new ThreadWorkerFactory(transport, pipeline, pipelineInvoker, backoffStrategy, rebusLoggerFactory, _options, c.Get<RebusBus>);
            });

            PossiblyRegisterDefault<IErrorTracker>(c =>
            {
                var settings = c.Get<SimpleRetryStrategySettings>();
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                return new InMemErrorTracker(settings.MaxDeliveryAttempts, rebusLoggerFactory, asyncTaskFactory);
            });

            PossiblyRegisterDefault<IRetryStrategy>(c =>
            {
                var transport = c.Get<ITransport>();
                var simpleRetryStrategySettings = c.Get<SimpleRetryStrategySettings>();
                var errorTracker = c.Get<IErrorTracker>();
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                return new SimpleRetryStrategy(transport, simpleRetryStrategySettings, errorTracker, rebusLoggerFactory);
            });

            PossiblyRegisterDefault(c => new SimpleRetryStrategySettings());

            PossiblyRegisterDefault<ITimeoutManager>(c => new InMemoryTimeoutManager());

            PossiblyRegisterDefault(c =>
            {
                var transport = c.Get<ITransport>();
                var timeoutManager = c.Get<ITimeoutManager>();
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
                return new HandleDeferredMessagesStep(timeoutManager, transport, _options, rebusLoggerFactory, asyncTaskFactory);
            });

            PossiblyRegisterDefault(c => c.Get<IRetryStrategy>().GetRetryStep());

            PossiblyRegisterDefault<IPipeline>(c =>
            {
                var serializer = c.Get<ISerializer>();
                var transport = c.Get<ITransport>();
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();

                return new DefaultPipeline()
                    .OnReceive(c.Get<IRetryStrategyStep>())
                    .OnReceive(c.Get<HandleDeferredMessagesStep>())
                    .OnReceive(new DeserializeIncomingMessageStep(serializer))
                    .OnReceive(new ActivateHandlersStep(c.Get<IHandlerActivator>()))
                    .OnReceive(new LoadSagaDataStep(c.Get<ISagaStorage>(), rebusLoggerFactory))
                    .OnReceive(new DispatchIncomingMessageStep())

                    .OnSend(new AssignGuidMessageIdStep())
                    .OnSend(new AssignReturnAddressStep(transport))
                    .OnSend(new AssignDateTimeOffsetHeader())
                    .OnSend(new FlowCorrelationIdStep())
                    .OnSend(new AutoHeadersOutgoingStep())
                    .OnSend(new AssignTypeHeaderStep())
                    .OnSend(new SerializeOutgoingMessageStep(serializer))
                    .OnSend(new ValidateOutgoingMessageStep())
                    .OnSend(new SendOutgoingMessageStep(transport, rebusLoggerFactory));
            });

            RegisterDecorator<IPipeline>(c => new PipelineCache(c.Get<IPipeline>()));

            PossiblyRegisterDefault(c => new BusLifetimeEvents());

            PossiblyRegisterDefault<IDataBus>(c => new DisabledDataBus());

            // configuration hack - keep these two bad boys around to have them available at the last moment before returning the built bus instance...
            Action startAction = null;

            PossiblyRegisterDefault(c => new RebusBus(
                c.Get<IWorkerFactory>(),
                c.Get<IRouter>(),
                c.Get<ITransport>(),
                c.Get<IPipeline>(),
                c.Get<IPipelineInvoker>(),
                c.Get<ISubscriptionStorage>(),
                _options,
                c.Get<IRebusLoggerFactory>(),
                c.Get<BusLifetimeEvents>(),
                c.Get<IDataBus>()));

            PossiblyRegisterDefault<IBus>(c =>
            {
                var bus = c.Get<RebusBus>();

                bus.Disposed += () =>
                {
                    var disposableInstances = c.TrackedInstances.OfType<IDisposable>().Reverse();

                    foreach (var disposableInstance in disposableInstances)
                    {
                        disposableInstance.Dispose();
                    }
                };

                var initializableInstances = c.TrackedInstances.OfType<IInitializable>();

                foreach (var initializableInstance in initializableInstances)
                {
                    initializableInstance.Initialize();
                }

                // and then we set the startAction
                startAction = () => bus.Start(_options.NumberOfWorkers);

                return bus;
            });

            _injectionist.Decorate<IHandlerActivator>(c =>
            {
                var handlerActivator = c.Get<IHandlerActivator>();
                var subscriptionStorage = c.Get<ISubscriptionStorage>();
                var internalHandlersContributor = new InternalHandlersContributor(handlerActivator, subscriptionStorage);
                return internalHandlersContributor;
            });

            var butResolutionResult = _injectionist.Get<IBus>();
            var busInstance = butResolutionResult.Instance;

            // if there is a container adapter among the tracked instances, hand it the bus instance
            var containerAdapter = butResolutionResult.TrackedInstances
                .OfType<IContainerAdapter>()
                .FirstOrDefault();

            containerAdapter?.SetBus(busInstance);

            // and NOW we are ready to start the bus if there is a startAction
            startAction?.Invoke();

            _hasBeenStarted = true;

            return busInstance;
        }

        void VerifyRequirements()
        {
            if (_hasBeenStarted)
            {
                throw new InvalidOperationException("This configurer has already had .Start() called on it - this is not allowed, because it cannot be guaranteed that configuration extensions make their registrations in a way that allows for being called more than once. If you need to create multiple bus instances, please wrap the configuration from Configure.With(...) and on in a function that you can call multiple times.");
            }

            if (!_injectionist.Has<ITransport>())
            {
                throw new ConfigurationErrorsException(
                    "No transport has been configured! You need to call .Transport(t => t.Use***) in order" +
                    " to select which kind of queueing system you want to use to transport messages. If" +
                    " you want something lightweight (possibly for testing?) you can use .Transport(t => t.UseInMemoryTransport(...))");
            }
        }

        void PossiblyRegisterDefault<TService>(Func<IResolutionContext, TService> factoryMethod)
        {
            if (_injectionist.Has<TService>()) return;

            _injectionist.Register(factoryMethod);
        }

        void RegisterDecorator<TService>(Func<IResolutionContext, TService> factoryMethod)
        {
            _injectionist.Decorate(factoryMethod);
        }
    }
}