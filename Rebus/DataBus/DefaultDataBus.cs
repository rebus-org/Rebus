using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus
{
    class DefaultDataBus : IDataBus
    {
        readonly IDataBusStorage _dataBusStorage;

        public DefaultDataBus(IDataBusStorage dataBusStorage)
        {
            _dataBusStorage = dataBusStorage ?? throw new ArgumentNullException(nameof(dataBusStorage));
        }

        public async Task<DataBusAttachment> CreateAttachment(Stream source, Dictionary<string, string> optionalMetadata = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var id = Guid.NewGuid().ToString();

            await _dataBusStorage.Save(id, source, optionalMetadata).ConfigureAwait(false);

            var attachment = new DataBusAttachment(id);

            return attachment;
        }

        public async Task<Stream> OpenRead(string dataBusAttachmentId) => await _dataBusStorage.Read(dataBusAttachmentId).ConfigureAwait(false);

        public async Task<Dictionary<string, string>> GetMetadata(string dataBusAttachmentId) => await _dataBusStorage.ReadMetadata(dataBusAttachmentId).ConfigureAwait(false);
    }
}