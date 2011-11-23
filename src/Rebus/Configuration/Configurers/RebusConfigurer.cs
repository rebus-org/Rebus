using System;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;

namespace Rebus.Configuration.Configurers
{
    public class RebusConfigurer
    {
        protected readonly IContainerAdapter containerAdapter;

        public RebusConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
            containerAdapter.RegisterInstance(containerAdapter, typeof (IActivateHandlers));
        }

        public RebusConfigurer Transport(Action<TransportConfigurer> configureTransport)
        {
            configureTransport(new TransportConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer Sagas(Action<SagaConfigurer> configureSagas)
        {
            configureSagas(new SagaConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer Subscriptions(Action<SubscriptionsConfigurer> configureSubscriptions)
        {
            configureSubscriptions(new SubscriptionsConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer Serialization(Action<SerializationConfigurer> configureSerialization)
        {
            configureSerialization(new SerializationConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer DetermineEndpoints(Action<EndpointMappingsConfigurer> configureEndpointsMappings)
        {
            configureEndpointsMappings(new EndpointMappingsConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer SpecifyOrderOfHandlers(Action<PipelineInspectorConfigurer> configurePipelineInspector)
        {
            configurePipelineInspector(new PipelineInspectorConfigurer(containerAdapter));
            return this;
        }

        public RebusConfigurer Discovery(Action<DiscoveryConfigurer> configureDiscovery)
        {
            configureDiscovery(new DiscoveryConfigurer(containerAdapter));
            return this;
        }

        public IStartableBus CreateBus()
        {
            FillOutMissingRegistrationsWithDefaults();
            
            ValidateConfiguration();

            return containerAdapter.Resolve<IStartableBus>();
        }

        void ValidateConfiguration()
        {
            if (!(containerAdapter.HasImplementationOf(typeof(ISendMessages))
                || containerAdapter.HasImplementationOf(typeof(IReceiveMessages))))
            {
                throw new ConfigurationException
                    (@"
You need to configure Rebus to be able to at least either SEND or RECEIVE messages. Otherwise
it wouldn't be that useful, would it?

If, for some reason, you really really WANT to circumvent this rule, please feel free to get
the bus by new'ing it up yourself - then you can do whatever you feel like.

This configuration API, however, will not let you create an unusable bus. You can configure
the transport in one easy operation like so:

    var bus = Configure.With(someContainerAdapter)
                .Transport(s => s.UseMsmq(""some_input_queue_name""))
                (....)
                .CreateBus()

thus configuring the ability to send AND receive messages at the same time, using MSMQ for
both.");
            }
        }

        void FillOutMissingRegistrationsWithDefaults()
        {
            if (!containerAdapter.HasImplementationOf(typeof(IStoreSubscriptions)))
            {
                containerAdapter.RegisterInstance(new InMemorySubscriptionStorage(), typeof(IStoreSubscriptions));
            }

            if (!containerAdapter.HasImplementationOf(typeof(IStoreSagaData)))
            {
                containerAdapter.RegisterInstance(new InMemorySagaPersister(), typeof(IStoreSagaData));
            }

            if (!containerAdapter.HasImplementationOf(typeof(IDetermineDestination)))
            {
                containerAdapter.RegisterInstance(new ThrowingEndpointMapper(), typeof(IDetermineDestination));
            }

            if (!containerAdapter.HasImplementationOf(typeof(ISerializeMessages)))
            {
                containerAdapter.RegisterInstance(new JsonMessageSerializer(), typeof (ISerializeMessages));
            }

            if (!containerAdapter.HasImplementationOf(typeof(IInspectHandlerPipeline)))
            {
                containerAdapter.RegisterInstance(new TrivialPipelineInspector(), typeof (IInspectHandlerPipeline));
            }

            if (!containerAdapter.HasImplementationOf(typeof(IStartableBus)))
            {
                if (containerAdapter.HasImplementationOf(typeof(IBus)))
                {
                    throw new ConfigurationException(@"
Detected that there was no IStartableBus configured, even though there is an IBus! If you want
to work with the configuration API, and you want to register your some custom implementation
of IBus and/or IStartableBus, please assume the full responsibility and register something
that implements both!

Registering only one of them will most likely lead to confusion some time in the future.
");
                }

                containerAdapter.Register(typeof(RebusBus), Lifestyle.Singleton, typeof(IStartableBus), typeof(IBus));
            }
            else
            {
                if (!containerAdapter.HasImplementationOf(typeof(IBus)))
                {
                    throw new ConfigurationException(
                        @"
Detected that there was no IBus configured, even though there is an IStartableBus! If you want
to work with the configuration API, and you want to register your some custom implementation
of IBus and/or IStartableBus, please assume the full responsibility and register something
that implements both!

Registering only one of them will most likely lead to confusion some time in the future.
");
                }
            }
        }
    }
}