using System;
using Rebus.Bus;

namespace Rebus.Config;

/// <summary>
/// Configuration extension that allows for delaying bus startup
/// </summary>
public static class DelayedStartupConfigurationExtensions
{
    /// <summary>
    /// Starts the bus with 0 workers, thus creating a fully functional bus, only without starting message processing.
    /// The returned <see cref="IBusStarter"/> can then be used to start the bus by calling <see cref="IBusStarter.Start"/>
    /// on it.
    /// </summary>
    public static IBusStarter Create(this RebusConfigurer configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        var desiredNumberOfWorkersWhenStarted = 0;

        var bus = configurer
            .Options(o =>
            {
                // modify Options by setting number of workers to 0
                o.Decorate(c =>
                {
                    var options = c.Get<Options>();

                    desiredNumberOfWorkersWhenStarted = options.NumberOfWorkers;

                    // delay bus start by doing this
                    options.NumberOfWorkers = 0;

                    return options;
                });
            })
            .Start();

        return new BusStarter(bus, desiredNumberOfWorkersWhenStarted);
    }

    sealed class BusStarter : IBusStarter
    {
        readonly IBus _bus;
        readonly int _desiredNumberOfWorkersWhenStarted;

        public BusStarter(IBus bus, int desiredNumberOfWorkersWhenStarted)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _desiredNumberOfWorkersWhenStarted = desiredNumberOfWorkersWhenStarted;
        }

        public IBus Bus => _bus;

        public IBus Start()
        {
            _bus.Advanced.Workers.SetNumberOfWorkers(_desiredNumberOfWorkersWhenStarted);

            return _bus;
        }
    }
}