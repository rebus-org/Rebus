using System;
using System.Threading.Tasks;
using Rebus.Pipeline;

namespace Rebus.DataBus
{
    class DataBusIncomingStep : IIncomingStep
    {
        public const string DataBusStorageKey = "rebus-databus-storage";

        readonly IDataBusStorage _dataBusStorage;

        public DataBusIncomingStep(IDataBusStorage dataBusStorage)
        {
            if (dataBusStorage == null) throw new ArgumentNullException(nameof(dataBusStorage));
            _dataBusStorage = dataBusStorage;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            // stashes the current implementation of IDataBusStorage in the context for DataBusAttachment to find
            context.Save(DataBusStorageKey, _dataBusStorage);

            await next();
        }
    }
}