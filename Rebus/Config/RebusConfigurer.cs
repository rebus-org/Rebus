using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Compression;
using Rebus.DataBus;
using Rebus.DataBus.ClaimCheck;
using Rebus.Exceptions;
using Rebus.Handlers;
using Rebus.Injection;
using Rebus.Logging;
using Rebus.Persistence.Throwing;
using Rebus.Pipeline;
using Rebus.Pipeline.Invokers;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Retry;
using Rebus.Retry.ErrorTracking;
using Rebus.Retry.PoisonQueues;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Serialization;
using Rebus.Serialization.Json;
using Rebus.Subscriptions;
using Rebus.Threading;
using Rebus.Threading.TaskParallelLibrary;
using Rebus.Timeouts;
using Rebus.Transport;
using Rebus.Workers;
using Rebus.Workers.ThreadPoolBased;
using Rebus.Retry.FailFast;
using Rebus.Time;
using Rebus.Topic;
// ReSharper disable EmptyGeneralCatchClause
// ReSharper disable ArgumentsStyleNamedExpression

namespace Rebus.Config;

/// <summary>
/// Basic skeleton of the fluent configuration builder. Contains a method for each aspect that can be configured
/// </summary>
public class RebusConfigurer
{
    readonly Injectionist _injectionist = new();
    readonly Options _options = new();

    bool _hasBeenStarted;

    internal RebusConfigurer(IHandlerActivator handlerActivator)
    {
        if (handlerActivator == null) throw new ArgumentNullException(nameof(handlerActivator));

        _injectionist.Register(_ => handlerActivator);

        if (handlerActivator is IContainerAdapter)
        {
            _injectionist.Register(_ => (IContainerAdapter)handlerActivator);
        }
    }

    /// <summary>
    /// Configures how Rebus logs stuff that happens
    /// </summary>
    public RebusConfigurer Logging(Action<RebusLoggingConfigurer> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer(new RebusLoggingConfigurer(_injectionist));
        return this;
    }

    /// <summary>
    /// Configures how Rebus sends/receives messages by allowing for choosing which implementation of <see cref="ITransport"/> to use
    /// </summary>
    public RebusConfigurer Transport(Action<StandardConfigurer<ITransport>> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer(new StandardConfigurer<ITransport>(_injectionist, _options));
        return this;
    }

    /// <summary>
    /// Configures how Rebus tracks <see cref="Exception"/>s.
    /// Defaults to tracking exceptions in memory. Trasking errors in memory is easy and does not require any additional configuration,
    /// but it can lead to excessive retrying in competing consumer scenarios, because each node will count delivery attempts individually.
    /// It is recommended in most cases to configure some kind of distributed error tracker when running distributed consumers.
    /// </summary>
    public RebusConfigurer Errors(Action<StandardConfigurer<IErrorTracker>> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer(new StandardConfigurer<IErrorTracker>(_injectionist, _options));
        return this;
    }

    /// <summary>
    /// Enables the data bus and configures which implementation of <see cref="IDataBusStorage"/> to use.
    /// </summary>
    public RebusConfigurer DataBus(Action<StandardConfigurer<IDataBusStorage>> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer(new StandardConfigurer<IDataBusStorage>(_injectionist, _options));

        if (_injectionist.Has<IDataBusStorage>())
        {
            if (!_injectionist.Has<IDataBusStorageManagement>())
            {
                _injectionist.Register<IDataBusStorageManagement>(_ => new DisabledDataBusStorageManagement());
            }

            _injectionist.Register<IDataBus>(c =>
            {
                var dataBusStorage = c.Get<IDataBusStorage>();
                var dataBusStorageManagement = c.Get<IDataBusStorageManagement>();

                return new DefaultDataBus(dataBusStorage, dataBusStorageManagement);
            });

            _injectionist.Decorate<IPipeline>(c =>
            {
                var dataBusStorage = c.Get<IDataBusStorage>();
                var pipeline = c.Get<IPipeline>();

                var step = new DataBusIncomingStep(dataBusStorage);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(step, PipelineRelativePosition.After, typeof(DeserializeIncomingMessageStep));
            });
        }

        return this;
    }

    /// <summary>
    /// Configures how Rebus routes messages by allowing for choosing which implementation of <see cref="IRouter"/> to use
    /// </summary>
    public RebusConfigurer Routing(Action<StandardConfigurer<IRouter>> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer(new StandardConfigurer<IRouter>(_injectionist, _options));
        return this;
    }

    /// <summary>
    /// Configures how Rebus persists saga data by allowing for choosing which implementation of <see cref="ISagaStorage"/> to use
    /// </summary>
    public RebusConfigurer Sagas(Action<StandardConfigurer<ISagaStorage>> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer(new StandardConfigurer<ISagaStorage>(_injectionist, _options));
        return this;
    }

    /// <summary>
    /// Configures how Rebus persists subscriptions by allowing for choosing which implementation of <see cref="ISubscriptionStorage"/> to use
    /// </summary>
    public RebusConfigurer Subscriptions(Action<StandardConfigurer<ISubscriptionStorage>> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer(new StandardConfigurer<ISubscriptionStorage>(_injectionist, _options));
        return this;
    }

    /// <summary>
    /// Configures how Rebus serializes messages by allowing for choosing which implementation of <see cref="ISerializer"/> to use
    /// </summary>
    public RebusConfigurer Serialization(Action<StandardConfigurer<ISerializer>> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer(new StandardConfigurer<ISerializer>(_injectionist, _options));
        return this;
    }

    /// <summary>
    /// Configures how Rebus defers messages to the future by allowing for choosing which implementation of <see cref="ITimeoutManager"/> to use
    /// </summary>
    public RebusConfigurer Timeouts(Action<StandardConfigurer<ITimeoutManager>> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        configurer(new StandardConfigurer<ITimeoutManager>(_injectionist, _options));
        return this;
    }

    /// <summary>
    /// Configures additional options about how Rebus works
    /// </summary>
    public RebusConfigurer Options(Action<OptionsConfigurer> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
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

        _injectionist.Register(_ => _options);
        _injectionist.Register(_ => new CancellationTokenSource());
        _injectionist.Register(c => c.Get<CancellationTokenSource>().Token);

        PossiblyRegisterDefault<IRebusLoggerFactory>(_ => new ConsoleLoggerFactory(true));

        PossiblyRegisterDefault<IRebusTime>(_ => new DefaultRebusTime());

        PossiblyRegisterDefault<IExceptionLogger>(c => new DefaultExceptionLogger(c.Get<IRebusLoggerFactory>()));

        //PossiblyRegisterDefault<IAsyncTaskFactory>(c => new TimerAsyncTaskFactory(c.Get<IRebusLoggerFactory>()));
        PossiblyRegisterDefault<IAsyncTaskFactory>(c =>
        {
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            return new TplAsyncTaskFactory(rebusLoggerFactory);
        });

        PossiblyRegisterDefault<IRouter>(c =>
        {
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            return new TypeBasedRouter(rebusLoggerFactory);
        });

        PossiblyRegisterDefault<ISubscriptionStorage>(_ => new DisabledSubscriptionStorage());

        PossiblyRegisterDefault<ISagaStorage>(_ => new DisabledSagaStorage());

        PossiblyRegisterDefault<ITimeoutManager>(_ => new DisabledTimeoutManager());

        PossiblyRegisterDefault<ISerializer>(c => new SystemTextJsonSerializer(c.Get<IMessageTypeNameConvention>()));
        //PossiblyRegisterDefault<ISerializer>(c => new JsonSerializer(c.Get<IMessageTypeNameConvention>()));

        PossiblyRegisterDefault<IPipelineInvoker>(c => new DefaultPipelineInvokerNew(c.Get<IPipeline>()));

        PossiblyRegisterDefault<IBackoffStrategy>(c =>
        {
            var backoffTimes = new[]
            {
                // 10 s
                Enumerable.Repeat(TimeSpan.FromMilliseconds(100), 10),

                // on and on
                Enumerable.Repeat(TimeSpan.FromMilliseconds(250), 1)
            };

            var options = c.Get<Options>();

            return new DefaultBackoffStrategy(backoffTimes.SelectMany(e => e), options);
        });

        PossiblyRegisterDefault<IWorkerFactory>(c =>
        {
            var transport = c.Get<ITransport>();
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            var pipelineInvoker = c.Get<IPipelineInvoker>();
            var options = c.Get<Options>();
            var busLifetimeEvents = c.Get<BusLifetimeEvents>();
            var backoffStrategy = c.Get<IBackoffStrategy>();
            var cancellationToken = c.Get<CancellationToken>();

            return new ThreadPoolWorkerFactory(
                transport: transport,
                rebusLoggerFactory: rebusLoggerFactory,
                pipelineInvoker: pipelineInvoker,
                options: options,
                busGetter: c.Get<RebusBus>,
                busLifetimeEvents: busLifetimeEvents,
                backoffStrategy: backoffStrategy,
                busDisposalCancellationToken: cancellationToken
            );
        });

        //PossiblyRegisterDefault<IWorkerFactory>(c =>
        //{
        //    var transport = c.Get<ITransport>();
        //    var loggerFactory = c.Get<IRebusLoggerFactory>();
        //    var pipelineInvoker = c.Get<IPipelineInvoker>();
        //    var options = c.Get<Options>();
        //    var busLifetimeEvents = c.Get<BusLifetimeEvents>();
        //    var backoffStrategy = c.Get<IBackoffStrategy>();
        //    return new TplWorkerFactory(transport, loggerFactory, pipelineInvoker, options, c.Get<RebusBus>, busLifetimeEvents, backoffStrategy);
        //});

        PossiblyRegisterDefault<IErrorTracker>(c =>
        {
            var settings = c.Get<RetryStrategySettings>();
            var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
            var rebusTime = c.Get<IRebusTime>();
            var exceptionLogger = c.Get<IExceptionLogger>();
            return new InMemErrorTracker(settings, asyncTaskFactory, rebusTime, exceptionLogger);
        });

        PossiblyRegisterDefault<IErrorHandler>(c =>
        {
            var settings = c.Get<RetryStrategySettings>();
            var transport = c.Get<ITransport>();
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            return new DeadletterQueueErrorHandler(settings, transport, rebusLoggerFactory);
        });

        PossiblyRegisterDefault<IFailFastChecker>(_ => new FailFastChecker());

        PossiblyRegisterDefault<IRetryStrategy>(c =>
        {
            var simpleRetryStrategySettings = c.Get<RetryStrategySettings>();
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            var errorTracker = c.Get<IErrorTracker>();
            var errorHandler = c.Get<IErrorHandler>();
            var failFastChecker = c.Get<IFailFastChecker>();
            var cancellationToken = c.Get<CancellationToken>();
            return new DefaultRetryStrategy(simpleRetryStrategySettings, rebusLoggerFactory, errorTracker, errorHandler, failFastChecker, cancellationToken);
        });

        PossiblyRegisterDefault(_ => new RetryStrategySettings());

        PossiblyRegisterDefault(c =>
        {
            var transport = c.Get<ITransport>();
            var timeoutManager = c.Get<ITimeoutManager>();
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            var asyncTaskFactory = c.Get<IAsyncTaskFactory>();
            return new HandleDeferredMessagesStep(timeoutManager, transport, _options, rebusLoggerFactory, asyncTaskFactory);
        });

        PossiblyRegisterDefault(c => c.Get<IRetryStrategy>().GetRetryStep());

        PossiblyRegisterDefault<ICorrelationErrorHandler>(c => new DefaultCorrelationErrorHandler(c.Get<IRebusLoggerFactory>()));

        PossiblyRegisterDefault<IPipeline>(c =>
        {
            var serializer = c.Get<ISerializer>();
            var transport = c.Get<ITransport>();
            var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
            var options = c.Get<Options>();
            var rebusTime = c.Get<IRebusTime>();
            var messageTypeNameConvention = c.Get<IMessageTypeNameConvention>();

            return new DefaultPipeline()

                .OnReceive(c.Get<IRetryStep>())
                .OnReceive(c.Get<HandleDeferredMessagesStep>())
                .OnReceive(new HydrateIncomingMessageStep(c.Get<IDataBus>()))
                .OnReceive(new DeserializeIncomingMessageStep(serializer))
                .OnReceive(new HandleRoutingSlipsStep(transport, serializer))
                .OnReceive(new ActivateHandlersStep(c.Get<IHandlerActivator>()))
                .OnReceive(new LoadSagaDataStep(c.Get<ISagaStorage>(), c.Get<ICorrelationErrorHandler>(), rebusLoggerFactory, options))
                .OnReceive(new DispatchIncomingMessageStep(rebusLoggerFactory))

                .OnSend(new AssignDefaultHeadersStep(transport, rebusTime, messageTypeNameConvention, options.DefaultReturnAddressOrNull))
                .OnSend(new FlowCorrelationIdStep())
                .OnSend(new AutoHeadersOutgoingStep())
                .OnSend(new SerializeOutgoingMessageStep(serializer))
                .OnSend(new ValidateOutgoingMessageStep())
                .OnSend(new SendOutgoingMessageStep(transport, rebusLoggerFactory));
        });

        PossiblyRegisterDefault(_ => new BusLifetimeEvents());

        PossiblyRegisterDefault<IDataBus>(_ => new DisabledDataBus());

        PossiblyRegisterDefault<ITopicNameConvention>(_ => new DefaultTopicNameConvention());

        PossiblyRegisterDefault<IMessageTypeNameConvention>(_ => new SimpleAssemblyQualifiedMessageTypeNameConvention());

        // configuration hack - keep these two bad boys around to have them available at the last moment before returning the built bus instance...
        Action startAction = null;

        PossiblyRegisterDefault(c => new RebusBus(
            c.Get<IWorkerFactory>(),
            c.Get<IRouter>(),
            c.Get<ITransport>(),
            c.Get<IPipelineInvoker>(),
            c.Get<ISubscriptionStorage>(),
            _options,
            c.Get<IRebusLoggerFactory>(),
            c.Get<BusLifetimeEvents>(),
            c.Get<IDataBus>(),
            c.Get<ITopicNameConvention>(),
            c.Get<IRebusTime>()
        ));

        // since an error during resolution does not give access to disposable instances, we need to do this
        var disposableInstancesTrackedFromInitialResolution = new ConcurrentStack<IDisposable>();

        PossiblyRegisterDefault<IBus>(c =>
        {
            try
            {
                var bus = c.Get<RebusBus>();
                var cancellationTokenSource = c.Get<CancellationTokenSource>();

                c.Get<BusLifetimeEvents>().BusDisposing += () => cancellationTokenSource.Cancel();

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
            }
            catch
            {
                // stash'em here quick!
                foreach (var disposable in c.TrackedInstances.OfType<IDisposable>())
                {
                    disposableInstancesTrackedFromInitialResolution.Push(disposable);
                }
                throw;
            }
        });

        _injectionist.Decorate<IHandlerActivator>(c =>
        {
            var handlerActivator = c.Get<IHandlerActivator>();
            var subscriptionStorage = c.Get<ISubscriptionStorage>();
            var internalHandlersContributor = new InternalHandlersContributor(handlerActivator, subscriptionStorage);
            return internalHandlersContributor;
        });

        _injectionist.Decorate<ISerializer>(c =>
        {
            var serializer = c.Get<ISerializer>();
            var zipper = new Zipper();
            var unzippingSerializerDecorator = new UnzippingSerializerDecorator(serializer, zipper);
            return unzippingSerializerDecorator;
        });

        try
        {
            var busResolutionResult = _injectionist.Get<IBus>();
            var busInstance = busResolutionResult.Instance;

            // if there is a container adapter among the tracked instances, hand it the bus instance
            var containerAdapter = busResolutionResult.TrackedInstances
                .OfType<IContainerAdapter>()
                .FirstOrDefault();

            containerAdapter?.SetBus(busInstance);

            // and NOW we are ready to start the bus if there is a startAction
            startAction?.Invoke();

            _hasBeenStarted = true;

            return busInstance;
        }
        catch
        {
            while (disposableInstancesTrackedFromInitialResolution.TryPop(out var disposable))
            {
                try
                {
                    disposable.Dispose();
                }
                catch { } //< disposables must never throw, but sometimes they do
            }
            throw;
        }
    }

    void VerifyRequirements()
    {
        if (_hasBeenStarted)
        {
            throw new InvalidOperationException("This configurer has already had .Start() called on it - this is not allowed, because it cannot be guaranteed that configuration extensions make their registrations in a way that allows for being called more than once. If you need to create multiple bus instances, please wrap the configuration from Configure.With(...) and on in a function that you can call multiple times.");
        }

        if (!_injectionist.Has<ITransport>())
        {
            throw new RebusConfigurationException(
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
}