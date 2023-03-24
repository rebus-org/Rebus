using System;
using Rebus.Config;
using Rebus.Subscriptions;

namespace Rebus.Transport.InMem;

/// <summary>
/// Configuration extensions for the in-mem transport
/// </summary>
public static class InMemTransportConfigurationExtensions
{
    /// <summary>
    /// Configures Rebus to use in-mem message queues, delivering/receiving from the specified <see cref="InMemNetwork"/>
    /// If <paramref name="registerSubscriptionStorage"/> is TRUE, the in-mem network will be used as a subscription storage too, thus providing support for pub/sub without additional configuration.
    /// If <paramref name="registerSubscriptionStorage"/> is FALSE, another subscription storage can be registered via <code>.Subscriptions(s => s.StoreIn(...))</code>, which can be useful e.g. in testing scenarios.
    /// </summary>
    public static void UseInMemoryTransport(this StandardConfigurer<ITransport> configurer, InMemNetwork network, string inputQueueName, bool registerSubscriptionStorage = true)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (network == null) throw new ArgumentNullException(nameof(network));
        if (inputQueueName == null) throw new ArgumentNullException(nameof(inputQueueName));

        configurer.OtherService<InMemTransport>()
            .Register(_ => new InMemTransport(network, inputQueueName));

        configurer.OtherService<ITransportInspector>()
            .Register(context => context.Get<InMemTransport>());

        if (registerSubscriptionStorage)
        {
            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(context => context.Get<InMemTransport>());
        }

        configurer.Register(context => context.Get<InMemTransport>());
    }

    /// <summary>
    /// Configures Rebus to use in-mem message queues, configuring this instance to be a one-way client.
    /// If <paramref name="registerSubscriptionStorage"/> is TRUE, the in-mem network will be used as a subscription storage too, thus providing support for pub/sub without additional configuration.
    /// If <paramref name="registerSubscriptionStorage"/> is FALSE, another subscription storage can be registered via <code>.Subscriptions(s => s.StoreIn(...))</code>, which can be useful e.g. in testing scenarios.
    /// </summary>
    public static void UseInMemoryTransportAsOneWayClient(this StandardConfigurer<ITransport> configurer, InMemNetwork network, bool registerSubscriptionStorage = true)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));
        if (network == null) throw new ArgumentNullException(nameof(network));

        configurer.OtherService<InMemTransport>()
            .Register(_ => new InMemTransport(network, null));

        if (registerSubscriptionStorage)
        {
            configurer
                .OtherService<ISubscriptionStorage>()
                .Register(context => context.Get<InMemTransport>());
        }

        configurer.Register(context => context.Get<InMemTransport>());

        OneWayClientBackdoor.ConfigureOneWayClient(configurer);
    }
}