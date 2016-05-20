using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus.InMem
{
    class InMemDataBusStorage : IDataBusStorage
    {
        readonly InMemDataStore _dataStore;

        public InMemDataBusStorage(InMemDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task Save(string id, Stream source)
        {
            using (var destination = new MemoryStream())
            {
                await source.CopyToAsync(destination);

                _dataStore.Save(id, destination.ToArray());
            }
        }

        public Stream Read(string id)
        {
            var source = new MemoryStream(_dataStore.Load(id));

            return source;
        }
    }
}