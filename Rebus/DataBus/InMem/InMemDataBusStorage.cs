using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rebus.Time;

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
                var bytes = destination.ToArray();

                var metadataToWrite = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>())
                {
                    [MetadataKeys.SaveTime] = RebusTime.Now.ToString("O"),
                    [MetadataKeys.Length] = bytes.Length.ToString()
                };

                _dataStore.Save(id, bytes, metadataToWrite);
            }
        }

        public async Task<Stream> Read(string id)
        {
            var now = RebusTime.Now;

            var metadata = new Dictionary<string, string>
            {
                {MetadataKeys.ReadTime, now.ToString("O") }
            };

            _dataStore.AddMetadata(id, metadata);

            var source = new MemoryStream(_dataStore.Load(id));

            return source;
        }

        public async Task<Dictionary<string, string>> ReadMetadata(string id)
        {
            return _dataStore.LoadMetadata(id);
        }
    }
}