using System.Threading;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Transport;
using Rebus.Workers.ThreadPoolBased;

namespace Rebus.Workers.TplBased;

/// <summary>
/// Configuration extensions for an experimental TPL-based worker factory
/// </summary>
public static class TplOptionsExtensions
{
    /// <summary>
    /// Replaces the worker factory with one based on TPL
    /// </summary>
    public static void UseTplToReceiveMessages(this OptionsConfigurer configurer)
    {
        configurer.Register<IWorkerFactory>(c =>
        {
            var transport = c.Get<ITransport>();
            var loggerFactory = c.Get<IRebusLoggerFactory>();

            return new TplWorkerFactory(
                transport,
                loggerFactory,
                c.Get<IPipelineInvoker>(),
                c.Get<Options>(),
                c.Get<RebusBus>,
                c.Get<BusLifetimeEvents>(),
                c.Get<IBackoffStrategy>(),
                c.Get<CancellationToken>()
            );
        });
    }
}