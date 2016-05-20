using System;
using System.IO;
using System.Threading.Tasks;
using Rebus.Config;

namespace Rebus.DataBus
{
    static class FakeDataBusTestExtensions
    {
        public static void StoreInMemory(this StandardConfigurer<IDataBusStorage> configurer, InMemDataStore inMemDataStore)
        {
            if (inMemDataStore == null) throw new ArgumentNullException(nameof(inMemDataStore));

            configurer.Register(c => new InMemDataBusStorage(inMemDataStore));
        }

        internal class InMemDataBusStorage : IDataBusStorage
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
}