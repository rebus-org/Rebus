using System;
using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus
{
    class DefaultDataBus : IDataBus
    {
        readonly IDataBusStorage _dataBusStorage;

        public DefaultDataBus(IDataBusStorage dataBusStorage)
        {
            _dataBusStorage = dataBusStorage;
        }

        public async Task<DataBusAttachment> CreateAttachment(Stream source)
        {
            var id = Guid.NewGuid().ToString();

            await _dataBusStorage.Save(id, source);

            return new DataBusAttachment(id);
        }
    }
}