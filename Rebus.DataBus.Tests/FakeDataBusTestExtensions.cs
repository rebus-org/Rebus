using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Rebus.Config;

namespace Rebus.DataBus.Tests
{
    static class FakeDataBusTestExtensions
    {
        public static void StoreInMemory(this StandardConfigurer<IDataBusStorage> configurer)
        {
            configurer.Register(c => new InMemDataBusStorage());
        }

        internal class InMemDataBusStorage : IDataBusStorage
        {
            readonly ConcurrentDictionary<string, byte[]> _data = new ConcurrentDictionary<string, byte[]>();

            public async Task Save(string id, Stream source)
            {
                using (var destination = new MemoryStream())
                {
                    await source.CopyToAsync(destination);
                    _data[id] = destination.ToArray();
                }
            }

            public async Task Load(string id, Stream destination)
            {
                if (!_data.ContainsKey(id))
                {
                    throw new ArgumentException($"Could not find data with ID {id}");
                }

                using (var source = new MemoryStream(_data[id]))
                {
                    await source.CopyToAsync(destination);
                }
            }
        }
    }
}