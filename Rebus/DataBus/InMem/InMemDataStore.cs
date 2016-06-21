using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rebus.Extensions;

namespace Rebus.DataBus.InMem
{
    /// <summary>
    /// In-mem implementation of a data store that can be shared among buses that use the in-mem data bus.
    /// A shared instance of this class should be passed to all endpoints when calling <see cref="InMemDataBusExtensions.StoreInMemory"/>
    /// </summary>
    public class InMemDataStore
    {
        readonly ConcurrentDictionary<string, InMemBlob> _data = new ConcurrentDictionary<string, InMemBlob>();

        /// <summary>
        /// Saves the given bytes under the given ID
        /// </summary>
        public void Save(string id, byte[] bytes, Dictionary<string, string> metadata)
        {
            _data[id] = new InMemBlob(metadata, bytes);
        }

        /// <summary>
        /// Loads the bytes with the given ID. Throws a <see cref="ArgumentException"/> if no
        /// such ID exists
        /// </summary>
        public byte[] Load(string id)
        {
            InMemBlob blob;

            if (!_data.TryGetValue(id, out blob))
            {
                throw new ArgumentException($"Could not find data with ID {id}");
            }

            return blob.Data;
        }

        public void AddMetadata(string id, Dictionary<string, string> metadata)
        {
            InMemBlob blob;

            if (!_data.TryGetValue(id, out blob))
            {
                throw new ArgumentException($"Could not find data with ID {id}");
            }

            blob.AddMetadata(metadata);
        }

        /// <summary>
        /// Loads the metadata for the data with the given ID. Throws a <see cref="ArgumentException"/> if no
        /// such ID exists
        /// </summary>
        public Dictionary<string, string> LoadMetadata(string id)
        {
            InMemBlob blob;

            if (!_data.TryGetValue(id, out blob))
            {
                throw new ArgumentException($"Could not find data with ID {id}");
            }

            return blob.Metadata.Clone();
        }

        class InMemBlob
        {
            public InMemBlob(Dictionary<string, string> metadata, byte[] data)
            {
                if (metadata == null) throw new ArgumentNullException(nameof(metadata));
                if (data == null) throw new ArgumentNullException(nameof(data));
                Metadata = metadata.Clone();
                Data = data;
            }

            public byte[] Data { get; }
            public Dictionary<string, string> Metadata { get; private set; }

            public void AddMetadata(Dictionary<string, string> metadata)
            {
                var newMetadata = Metadata.Clone();

                foreach (var kvp in metadata)
                {
                    newMetadata[kvp.Key] = kvp.Value;
                }

                Metadata = newMetadata;
            }
        }
    }
}