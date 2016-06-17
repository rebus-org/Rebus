using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
#pragma warning disable 1998

namespace Rebus.DataBus.InMem
{
    class InMemDataBusStorage : IDataBusStorage
    {
        readonly InMemDataStore _dataStore;

        public InMemDataBusStorage(InMemDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task Save(string id, Stream source, Dictionary<string, string> metadata = null)
        {
            using (var destination = new MemoryStream())
            {
                await source.CopyToAsync(destination);

                var metadataToWrite = metadata ?? new Dictionary<string, string>();

                _dataStore.Save(id, destination.ToArray(), metadataToWrite);
            }
        }

        public Stream Read(string id)
        {
            var source = new MemoryStream(_dataStore.Load(id));

            return source;
        }

        public async Task<Dictionary<string, string>> ReadMetadata(string id)
        {
            return _dataStore.LoadMetadata(id);
        }
    }
}