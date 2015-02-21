using System;
using System.Configuration;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Injection;
using Rebus2.Logging;
using Rebus2.Persistence.InMem;
using Rebus2.Pipeline;
using Rebus2.Pipeline.Receive;
using Rebus2.Pipeline.Send;
using Rebus2.Retry;
using Rebus2.Retry.Simple;
using Rebus2.Routing;
using Rebus2.Routing.TypeBased;
using Rebus2.Serialization;
using Rebus2.Transport;
using Rebus2.Transport.InMem;
using Rebus2.Workers;

namespace Rebus2.Config
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
            if (!_injectionist.Has<ITransport>())
            {
                throw new ConfigurationErrorsException(
                    "No transport has been configured! You need to call .Transport(t => t.Use***) in order to select which kind of queueing system you want to use to transport messages. If you want something lightweight (possibly for testing?) you can use .Transport(t => t.UseInMemoryTransport(...))");
            }

            PossiblyRegisterDefault<IRouter>(c => new SimpleTypeBasedRouter());

            PossiblyRegisterDefault<ISubscriptionStorage>(c => new InMemorySubscriptionStorage(
                c.Get<IRouter>(), 
                c.Get<ITransport>(), 
                c.Get<IPipeline>(), 
                c.Get<IPipelineInvoker>(), 
                c.Get<ISerializer>()));
            
            PossiblyRegisterDefault<ISerializer>(c => new JsonSerializer());

            PossiblyRegisterDefault<IPipelineInvoker>(c => new DefaultPipelineInvoker());

            PossiblyRegisterDefault<IWorkerFactory>(c => new ThreadWorkerFactory(c.Get<ITransport>(), c.Get<IPipeline>(), c.Get<IPipelineInvoker>()));

            PossiblyRegisterDefault<IRetryStrategy>(c => new SimpleRetryStrategy(c.Get<ITransport>(), c.Get<SimpleRetryStrategySettings>()));

            PossiblyRegisterDefault(c => new SimpleRetryStrategySettings());

            PossiblyRegisterDefault<IPipeline>(c => new DefaultPipeline()

                .OnReceive(c.Get<IRetryStrategy>().GetRetryStep(), ReceiveStage.TransportMessageReceived)
                .OnReceive(new DeserializationStep(c.Get<ISerializer>()), ReceiveStage.TransportMessageReceived)
                .OnReceive(new DispatchStep(c.Get<IHandlerActivator>()), ReceiveStage.MessageDeserialized)

                .OnSend(new AssignGuidMessageIdStep())
                .OnSend(new AssignReturnAddressStep(c.Get<ITransport>()))

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

            return _injectionist.Get<IBus>();
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