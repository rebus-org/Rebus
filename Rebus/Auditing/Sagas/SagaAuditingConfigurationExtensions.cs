using System;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Injection;
using Rebus.Logging;
using Rebus.Pipeline;
using Rebus.Sagas;
using Rebus.Transport;

namespace Rebus.Auditing.Sagas;

/// <summary>
/// Configuration extensions for the auditing configuration
/// </summary>
public static class SagaAuditingConfigurationExtensions
{
    /// <summary>
    /// Enables saga auditing which will cause Rebus to save a snapshot of each saga state to the configured snapshot storage.
    /// Please remember to select a saga snapshot storage by calling an extension on the returned <see cref="StandardConfigurer{TService}"/>, e.g. like so:
    /// <code>
    /// Configure.With(..)
    ///     .(...)
    ///     .Options(o => o.EnableSagaAuditing().StoreInSqlServer(....))
    ///     .Start();
    /// </code>
    /// </summary>
    public static StandardConfigurer<ISagaSnapshotStorage> EnableSagaAuditing(this OptionsConfigurer configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer.Decorate<IPipeline>(c =>
        {
            var pipeline = c.Get<IPipeline>();
            var sagaSnapshotStorage = GetSagaSnapshotStorage(c);
            var transport = GetTransport(c);

            return new PipelineStepInjector(pipeline)
                .OnReceive(new SaveSagaDataSnapshotStep(sagaSnapshotStorage, transport), PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
        });

        return StandardConfigurer<ISagaSnapshotStorage>.GetConfigurerFrom(configurer);
    }

    /// <summary>
    /// Configures Rebus to output saga snapshots to the log. Each saga data mutation will be logged as a serialized JSON object without type information
    /// with INFO level to the class-logger of <see cref="LoggerSagaSnapperShotter"/>.
    /// This is probably mostly useful in debugging scenarios, or as a simple auditing mechanism in cases where sagas don't expect a lot of traffic.
    /// </summary>
    public static void OutputToLog(this StandardConfigurer<ISagaSnapshotStorage> configurer)
    {
        if (configurer == null) throw new ArgumentNullException(nameof(configurer));

        configurer.Register(c => new LoggerSagaSnapperShotter(c.Get<IRebusLoggerFactory>()));
    }

    static ITransport GetTransport(IResolutionContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        try
        {
            return context.Get<ITransport>();
        }
        catch (Exception exception)
        {
            throw new RebusApplicationException(exception, @"Could not get transport - did you call 'EnableSagaAuditing' on a one-way client? (which is not capable of receiving messages, and therefore can never get to change the stage of any saga instances...)");
        }
    }

    static ISagaSnapshotStorage GetSagaSnapshotStorage(IResolutionContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        try
        {
            return context.Get<ISagaSnapshotStorage>();
        }
        catch (Exception exception)
        {
            throw new RebusApplicationException(exception, @"Could not get saga snapshot storage - did you call 'EnableSagaAuditing' without choosing a way to store the snapshots?

When you enable the saving of saga data snapshots, you must specify how to save them - it can be done by making further calls after 'EnableSagaAuditing', e.g. like so:

Configure.With(..)
    .(...)
    .Options(o => o.EnableSagaAuditing().StoreInSqlServer(....))
    .(...)");
        }
    }
}