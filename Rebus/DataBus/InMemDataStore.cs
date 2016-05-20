using System;
using System.Collections.Concurrent;

namespace Rebus.DataBus
{
    /// <summary>
    /// In-mem implementation of a data store that can be shared among buses that use the in-mem data bus.
    /// </summary>
    public class InMemDataStore
    {
        readonly ConcurrentDictionary<string, byte[]> _data = new ConcurrentDictionary<string, byte[]>();

        /// <summary>
        /// Saves the given bytes under the given ID
        /// </summary>
        public void Save(string id, byte[] bytes)
        {
            _data[id] = bytes;
        }

        /// <summary>
        /// Loads the bytes with the given ID. Throws a <see cref="ArgumentException"/> if no
        /// such ID exists
        /// </summary>
        public byte[] Load(string id)
        {
            byte[] bytes;

            if (!_data.TryGetValue(id, out bytes))
            {
                throw new ArgumentException($"Could not find data with ID {id}");
            }

            return bytes;
        }
    }
}