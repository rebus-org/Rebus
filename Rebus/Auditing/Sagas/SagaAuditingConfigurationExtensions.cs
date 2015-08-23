using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Injection;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Sagas;

namespace Rebus.Auditing.Sagas
{
    /// <summary>
    /// Configuration extensions for the auditing configuration
    /// </summary>
    public static class SagaAuditingConfigurationExtensions
    {
        /// <summary>
        /// Enables message auditing whereby Rebus will forward to the audit queue a copy of each properly handled message and
        /// each published message
        /// </summary>
        public static StandardConfigurer<ISagaSnapshotStorage> EnableSagaAuditing(this OptionsConfigurer configurer)
        {
            configurer.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var sagaSnapshotStorage = GetSagaSnapshotStorage(c);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(new SaveSagaDataSnapshotStep(sagaSnapshotStorage), PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
            });

            return configurer.GetConfigurer<ISagaSnapshotStorage>();
        }

        static ISagaSnapshotStorage GetSagaSnapshotStorage(IResolutionContext c)
        {
            try
            {
                return c.Get<ISagaSnapshotStorage>();
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

    class SaveSagaDataSnapshotStep : IIncomingStep
    {
        readonly ISagaSnapshotStorage _sagaSnapshotStorage;

        public SaveSagaDataSnapshotStep(ISagaSnapshotStorage sagaSnapshotStorage)
        {
            _sagaSnapshotStorage = sagaSnapshotStorage;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            await next();

            var handlerInvokers = context.Load<HandlerInvokers>();

            var createdAndUpdatedSagaData = handlerInvokers
                .Where(i => i.HasSaga)
                .Select(i => i.GetSagaData())
                .ToList();

            var saveTasks = createdAndUpdatedSagaData
                .Select(sagaData => _sagaSnapshotStorage.Save(sagaData));

            await Task.WhenAll(saveTasks);
        }
    }
}