using Rebus.Bus;
using Rebus.Logging;
using Rebus.Transport;

namespace Rebus.Config
{
    /// <summary>
    /// Helper that gives a backdoor to the configuration <see cref="Options"/>, allowing for one-way client settings
    /// to be set.
    /// </summary>
    public class OneWayClientBackdoor
    {
        const string OneWayDecoratorDescription = "IBus was decorated with OneWayClientBusDecorator in order to disable the ability to change the number of workers";

        /// <summary>
        /// Uses the given <see cref="StandardConfigurer{TService}"/> of <see cref="ITransport"/> to set the number of workers
        /// to zero (effectively disabling message processing) and installs a decorator of <see cref="IBus"/> that prevents
        /// further modification of the number of workers (thus preventing accidentally starting workers when there's no input queue).
        /// </summary>
        public static void ConfigureOneWayClient(StandardConfigurer<ITransport> configurer)
        {
            configurer.Options.NumberOfWorkers = 0;

            configurer.OtherService<IBus>().Decorate(c =>
            {
                configurer.Options.NumberOfWorkers = 0;
                var realBus = c.Get<IBus>();
                var rebusLoggerFactory = c.Get<IRebusLoggerFactory>();
                return new OneWayClientBusDecorator(realBus, rebusLoggerFactory);
            }, description: OneWayDecoratorDescription);
        } 
    }
}