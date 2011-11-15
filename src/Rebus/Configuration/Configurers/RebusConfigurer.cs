using System;

namespace Rebus.Configuration.Configurers
{
    public class RebusConfigurer
    {
        readonly IContainerAdapter containerAdapter;

        public RebusConfigurer(IContainerAdapter containerAdapter)
        {
            this.containerAdapter = containerAdapter;
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
    }
}