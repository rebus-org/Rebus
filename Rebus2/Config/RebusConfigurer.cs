using System;
using Rebus2.Activation;
using Rebus2.Bus;
using Rebus2.Injection;
using Rebus2.Logging;
using Rebus2.Pipeline;
using Rebus2.Pipeline.Receive;
using Rebus2.Routing;
using Rebus2.Routing.TypeBased;
using Rebus2.Serialization;
using Rebus2.Transport;
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
            configurer(new OptionsConfigurer(_options));
            return this;
        }

        public IBus Start()
        {
            PossiblyRegisterDefault<IRouter>(c => new SimpleTypeBasedRouter());

            PossiblyRegisterDefault<ISerializer>(c => new JsonSerializer());

            PossiblyRegisterDefault<IWorkerFactory>(c => new ThreadWorkerFactory(c.Get<ITransport>(), c.Get<IPipeline>()));

            PossiblyRegisterDefault<IPipeline>(c => new DefaultPipeline()
                .OnReceive(new DeserializationStep(c.Get<ISerializer>()), ReceiveStage.TransportMessageReceived)
                .OnReceive(new DispatchStep(c.Get<IHandlerActivator>()), ReceiveStage.MessageDeserialized));

            PossiblyRegisterDefault<IBus>(c =>
            {
                var bus = new RebusBus(c.Get<IWorkerFactory>(),
                    c.Get<IRouter>(),
                    c.Get<ITransport>(),
                    c.Get<ISerializer>(),
                    c.Get<IPipeline>());

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

        public void Register(Func<IResolutionContext, TService> factoryMethod)
        {
            _injectionist.Register(factoryMethod);
        }
    }
}