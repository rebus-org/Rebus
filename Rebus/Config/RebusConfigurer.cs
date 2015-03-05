using System;
using System.Configuration;
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
using Rebus.Transport;
using Rebus.Workers;

namespace Rebus.Config
{
    public class RebusConfigurer
    {
        readonly Injectionist _injectionist = new Injectionist();
        readonly Options _options = new Options();

        public RebusConfigurer(IHandlerActivator handlerActivator)
        {
            _injectionist.Register(c => handlerActivator);
        }

        public RebusConfigurer Logging(Action<RebusLoggingConfigurer> configurer)
        {
            configurer(new RebusLoggingConfigurer());
            return this;
        }

        public RebusConfigurer Transport(Action<StandardConfigurer<ITransport>> configurer)
        {
            configurer(new StandardConfigurer<ITransport>(_injectionist));
            return this;
        }

        public RebusConfigurer Routing(Action<StandardConfigurer<IRouter>> configurer)
        {
            configurer(new StandardConfigurer<IRouter>(_injectionist));
            return this;
        }

        public RebusConfigurer Options(Action<OptionsConfigurer> configurer)
        {
            configurer(new OptionsConfigurer(_options, _injectionist));
            return this;
        }

        public IBus Start()
        {
            VerifyRequirements();

            PossiblyRegisterDefault<IRouter>(c => new TypeBasedRouter());

            PossiblyRegisterDefault<ISubscriptionStorage>(c => new InMemorySubscriptionStorage(
                c.Get<IRouter>(), 
                c.Get<ITransport>(), 
                c.Get<ISerializer>()));

            PossiblyRegisterDefault<ISagaStorage>(c => new InMemorySagaStorage());
            
            PossiblyRegisterDefault<ISerializer>(c => new JsonSerializer());

            PossiblyRegisterDefault<IPipelineInvoker>(c => new DefaultPipelineInvoker());

            PossiblyRegisterDefault<IWorkerFactory>(c =>
            {
                var factory = new ThreadWorkerFactory(c.Get<ITransport>(), c.Get<IPipeline>(), c.Get<IPipelineInvoker>())
                {
                    MaxParallelismPerWorker = _options.MaxParallelism
                };
                return factory;
            });

            PossiblyRegisterDefault<IRetryStrategy>(c => new SimpleRetryStrategy(c.Get<ITransport>(), c.Get<SimpleRetryStrategySettings>()));

            PossiblyRegisterDefault(c => new SimpleRetryStrategySettings());

            PossiblyRegisterDefault<IPipeline>(c => new DefaultPipeline()

                .OnReceive(c.Get<IRetryStrategy>().GetRetryStep(), ReceiveStage.TransportMessageReceived)
                .OnReceive(new DeserializeIncomingMessageStep(c.Get<ISerializer>()), ReceiveStage.TransportMessageReceived)
                .OnReceive(new ActivateHandlersStep(c.Get<IHandlerActivator>()), ReceiveStage.TransportMessageReceived)
                .OnReceive(new LoadSagaDataStep(c.Get<ISagaStorage>()), ReceiveStage.TransportMessageReceived)
                .OnReceive(new DispatchIncomingMessageStep(), ReceiveStage.MessageDeserialized)

                .OnSend(new AssignGuidMessageIdStep())
                .OnSend(new AssignReturnAddressStep(c.Get<ITransport>()))
                .OnSend(new FlowCorrelationIdStep())
                .OnSend(new SerializeOutgoingMessageStep(c.Get<ISerializer>()))
                .OnSend(new SendOutgoingMessageStep(c.Get<ITransport>()))

                );

            PossiblyRegisterDefault<IBus>(c =>
            {
                var bus = new RebusBus(
                    c.Get<IWorkerFactory>(),
                    c.Get<IRouter>(),
                    c.Get<ITransport>(),
                    c.Get<ISerializer>(),
                    c.Get<IPipeline>(),
                    c.Get<IPipelineInvoker>(),
                    c.Get<ISubscriptionStorage>());

                bus.Start(_options.NumberOfWorkers);

                return bus;
            });

            _injectionist.Register<IHandlerActivator>(c => new InternalHandlersContributor(c.Get<IHandlerActivator>(), c.Get<ISubscriptionStorage>()), isDecorator: true);

            return _injectionist.Get<IBus>();
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
    }

    public class RebusLoggingConfigurer
    {
        public void Console(LogLevel minLevel = LogLevel.Debug)
        {
            UseLoggerFactory(new ConsoleLoggerFactory(false)
            {
                MinLevel = minLevel
            });
        }

        public void ColoredConsole(LogLevel minLevel = LogLevel.Debug)
        {
            UseLoggerFactory(new ConsoleLoggerFactory(true)
            {
                MinLevel = minLevel
            });
        }

        public void Trace()
        {
            UseLoggerFactory(new TraceLoggerFactory());
        }

        public void None()
        {
            UseLoggerFactory(new NullLoggerFactory());
        }

        static void UseLoggerFactory(IRebusLoggerFactory consoleLoggerFactory)
        {
            RebusLoggerFactory.Current = consoleLoggerFactory;
        }
    }

    public class StandardConfigurer<TService>
    {
        readonly Injectionist _injectionist;

        public StandardConfigurer(Injectionist injectionist)
        {
            _injectionist = injectionist;
        }

        internal void Register(Func<IResolutionContext, TService> factoryMethod)
        {
            _injectionist.Register(factoryMethod);
        }
    }
}