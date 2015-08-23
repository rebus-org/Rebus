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
using Rebus.Transport;

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
                var transport = GetTransport(c);

                return new PipelineStepInjector(pipeline)
                    .OnReceive(new SaveSagaDataSnapshotStep(sagaSnapshotStorage, transport), PipelineRelativePosition.Before, typeof(LoadSagaDataStep));
            });

            return configurer.GetConfigurer<ISagaSnapshotStorage>();
        }

        static ITransport GetTransport(IResolutionContext c)
        {
            try
            {
                return c.Get<ITransport>();
            }
            catch (Exception exception)
            {
                throw new RebusApplicationException(exception, @"Could not get transport - did you call 'EnableSagaAuditing' on a one-way client? (which is not capable of receiving messages, and therefore can never get to change the stage of any saga instances...)");
            }
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
        readonly ITransport _transport;

        public SaveSagaDataSnapshotStep(ISagaSnapshotStorage sagaSnapshotStorage, ITransport transport)
        {
            _sagaSnapshotStorage = sagaSnapshotStorage;
            _transport = transport;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            await next();

            var handlerInvokers = context.Load<HandlerInvokers>();

            var createdAndUpdatedSagaData = handlerInvokers
                .Where(i => i.HasSaga)
                .Select(i => i.GetSagaData())
                .ToList();

            var metadata = GetMetadata();

            var saveTasks = createdAndUpdatedSagaData
                .Select(sagaData => _sagaSnapshotStorage.Save(sagaData, metadata));

            await Task.WhenAll(saveTasks);
        }

        Dictionary<string,string> GetMetadata()
        {
            return new Dictionary<string, string>
            {
                {"handlequeue", _transport.Address}
            };
        }
    }
}