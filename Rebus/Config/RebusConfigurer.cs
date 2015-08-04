using System;
using System.Configuration;
using System.Linq;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Injection;
using Rebus.Logging;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Retry;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Subscriptions;
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

        internal RebusConfigurer(IHandlerActivator handlerActivator)
        {
            _injectionist.Register(c => handlerActivator);

            if (handlerActivator is IContainerAdapter)
            {
                _injectionist.Register(c => (IContainerAdapter)handlerActivator);
            }
        }

        /// <summary>
        /// Configures how Rebus logs things that happen by installing a <see cref="RebusLoggerFactory"/> instance
        /// on <see cref="RebusLoggerFactory.Current"/>
        /// </summary>
        public RebusConfigurer Logging(Action<RebusLoggingConfigurer> configurer)
        {
            configurer(new RebusLoggingConfigurer());
            return this;
        }

        /// <summary>
        /// Configures how Rebus sends/receives messages by allowing for choosing which implementation of <see cref="ITransport"/> to use
        /// </summary>
        public RebusConfigurer Transport(Action<StandardConfigurer<ITransport>> configurer)
        {
            configurer(new StandardConfigurer<ITransport>(_injectionist));
            return this;
        }

        /// <summary>
        /// Configures how Rebus routes messages by allowing for choosing which implementation of <see cref="IRouter"/> to use
        /// </summary>
        public RebusConfigurer Routing(Action<StandardConfigurer<IRouter>> configurer)
        {
            configurer(new StandardConfigurer<IRouter>(_injectionist));
            return this;
        }

        /// <summary>
        /// Configures how Rebus persists saga data by allowing for choosing which implementation of <see cref="ISagaStorage"/> to use
        /// </summary>
        public RebusConfigurer Sagas(Action<StandardConfigurer<ISagaStorage>> configurer)
        {
            configurer(new StandardConfigurer<ISagaStorage>(_injectionist));
            return this;
        }

        /// <summary>
        /// Configures how Rebus persists subscriptions by allowing for choosing which implementation of <see cref="ISubscriptionStorage"/> to use
        /// </summary>
        public RebusConfigurer Subscriptions(Action<StandardConfigurer<ISubscriptionStorage>> configurer)
        {
            configurer(new StandardConfigurer<ISubscriptionStorage>(_injectionist));
            return this;
        }

        /// <summary>
        /// Configures how Rebus serializes messages by allowing for choosing which implementation of <see cref="ISerializer"/> to use
        /// </summary>
        public RebusConfigurer Serialization(Action<StandardConfigurer<ISerializer>> configurer)
        {
            configurer(new StandardConfigurer<ISerializer>(_injectionist));
            return this;
        }

        /// <summary>
        /// Configures how Rebus defers messages to the future by allowing for choosing which implementation of <see cref="ITimeoutManager"/> to use
        /// </summary>
        public RebusConfigurer Timeouts(Action<StandardConfigurer<ITimeoutManager>> configurer)
        {
            configurer(new StandardConfigurer<ITimeoutManager>(_injectionist));
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

            PossiblyRegisterDefault<IRouter>(c => new TypeBasedRouter());

            PossiblyRegisterDefault<ISubscriptionStorage>(c => new InMemorySubscriptionStorage());

            PossiblyRegisterDefault<ISagaStorage>(c => new InMemorySagaStorage());

            PossiblyRegisterDefault<ISerializer>(c => new JsonSerializer());

            PossiblyRegisterDefault<IPipelineInvoker>(c => new DefaultPipelineInvoker());

            PossiblyRegisterDefault<IWorkerFactory>(c =>
            {
                var transport = c.Get<ITransport>();
                var pipeline = c.Get<IPipeline>();
                var pipelineInvoker = c.Get<IPipelineInvoker>();
                return new ThreadWorkerFactory(transport, pipeline, pipelineInvoker, _options.MaxParallelism);
            });

            PossiblyRegisterDefault<IRetryStrategy>(c =>
            {
                var transport = c.Get<ITransport>();
                var simpleRetryStrategySettings = c.Get<SimpleRetryStrategySettings>();
                return new SimpleRetryStrategy(transport, simpleRetryStrategySettings);
            });

            PossiblyRegisterDefault(c => new SimpleRetryStrategySettings());

            PossiblyRegisterDefault<ITimeoutManager>(c => new InMemoryTimeoutManager());

            PossiblyRegisterDefault(c =>
            {
                var transport = c.Get<ITransport>();
                var timeoutManager = c.Get<ITimeoutManager>();
                return new HandleDeferredMessagesStep(timeoutManager, transport, _options);
            });

            PossiblyRegisterDefault(c => c.Get<IRetryStrategy>().GetRetryStep());

            PossiblyRegisterDefault<IPipeline>(c => new DefaultPipeline()

                .OnReceive(c.Get<IRetryStrategyStep>())
                .OnReceive(c.Get<HandleDeferredMessagesStep>())
                .OnReceive(new DeserializeIncomingMessageStep(c.Get<ISerializer>()))
                .OnReceive(new ActivateHandlersStep(c.Get<IHandlerActivator>()))
                .OnReceive(new LoadSagaDataStep(c.Get<ISagaStorage>()))
                .OnReceive(new DispatchIncomingMessageStep())

                .OnSend(new AssignGuidMessageIdStep())
                .OnSend(new AssignReturnAddressStep(c.Get<ITransport>()))
                .OnSend(new AssignDateTimeOffsetHeader())
                .OnSend(new FlowCorrelationIdStep())
                .OnSend(new SerializeOutgoingMessageStep(c.Get<ISerializer>()))
                .OnSend(new SendOutgoingMessageStep(c.Get<ITransport>()))
                );

            RegisterDecorator<IPipeline>(c => new PipelineCache(c.Get<IPipeline>()));

            PossiblyRegisterDefault<IBus>(c =>
            {
                var bus = new RebusBus(
                    c.Get<IWorkerFactory>(),
                    c.Get<IRouter>(),
                    c.Get<ITransport>(),
                    c.Get<IPipeline>(),
                    c.Get<IPipelineInvoker>(),
                    c.Get<ISubscriptionStorage>(),
                    _options);

                bus.Disposed += () =>
                {
                    var disposableInstances = c.GetTrackedInstancesOf<IDisposable>().Reverse();

                    foreach (var disposableInstance in disposableInstances)
                    {
                        disposableInstance.Dispose();
                    }
                };

                var initializableInstances = c.GetTrackedInstancesOf<IInitializable>();

                foreach (var initializableInstance in initializableInstances)
                {
                    initializableInstance.Initialize();
                }

                if (_injectionist.Has<IContainerAdapter>())
                {
                    c.Get<IContainerAdapter>().SetBus(bus);
                }

                bus.Start(_options.NumberOfWorkers);

                return bus;
            });

            _injectionist.Decorate<IHandlerActivator>(c =>
            {
                var handlerActivator = c.Get<IHandlerActivator>();
                var subscriptionStorage = c.Get<ISubscriptionStorage>();
                var internalHandlersContributor = new InternalHandlersContributor(handlerActivator, subscriptionStorage);
                return internalHandlersContributor;
            });

            var busInstance = _injectionist.Get<IBus>();

            return busInstance;
        }

        void VerifyRequirements()
        {
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