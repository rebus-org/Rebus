using System;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Transport;

namespace Rebus.Config;

/// <summary>
/// Helper that gives a backdoor to the configuration <see cref="Options"/>, allowing for one-way client settings
/// to be set.
/// </summary>
public static class OneWayClientBackdoor
{
    const string OneWayDecoratorDescription = "IBus was decorated with OneWayClientBusDecorator in order to disable the ability to change the number of workers";

    /// <summary>
    /// Uses the given <see cref="StandardConfigurer{TService}"/> of <see cref="ITransport"/> to set the number of workers
    /// to zero (effectively disabling message processing) and installs a decorator of <see cref="IBus"/> that prevents
    /// further modification of the number of workers (thus preventing accidentally starting workers when there's no input queue).
    /// </summary>
    public static void ConfigureOneWayClient(StandardConfigurer<ITransport> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer.Options.NumberOfWorkers = 0;

        configurer.OtherService<IBus>()
            .Decorate(c =>
                {
                    var transport = c.Get<ITransport>();

                    if (transport.Address != null)
                    {
                        throw new InvalidOperationException(
                            $"Cannot configure this bus to be a one-way client, because the transport is configured with '{transport.Address}' as its input queue. One-way clients must have a NULL input queue, otherwise the transport could be fooled into believing it was supposed to receive messages");
                    }

                    var options = c.Get<Options>();
              options.NumberOfWorkers = 0;

                    var realBus = c.Get<IBus>();
                    var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                    var busDecorator = new OneWayClientBusDecorator(realBus, rebusLoggerFactory);

                    return busDecorator;
                },
                description: OneWayDecoratorDescription
            );
    } 
}