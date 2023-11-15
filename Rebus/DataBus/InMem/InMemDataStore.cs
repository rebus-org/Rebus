using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Rebus.Extensions;

namespace Rebus.DataBus.InMem;

/// <summary>
/// In-mem implementation of a data store that can be shared among buses that use the in-mem data bus.
/// A shared instance of this class should be passed to all endpoints when calling <see cref="InMemDataBusExtensions.StoreInMemory"/>
/// </summary>
public class InMemDataStore
{
    readonly ConcurrentDictionary<string, InMemBlob> _data = new();

    /// <summary>
    /// Gets the total size in bytes 
    /// </summary>
    public long SizeBytes => _data.Values.Sum(kvp => kvp.Data.Length);

    /// <summary>
    /// Gets all IDs for which data is stored.
    /// </summary>
    public IEnumerable<string> AttachmentIds => _data.Keys;

    /// <summary>
    /// Saves the given bytes under the given ID
    /// </summary>
    public void Save(string id, byte[] bytes, Dictionary<string, string> metadata = null)
    {
        _data[id] = new InMemBlob(metadata ?? new Dictionary<string, string>(), bytes);
    }

    /// <summary>
    /// Determines whether there is some data with the given ID.
    /// </summary>
    public bool Contains(string id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
            
        return _data.ContainsKey(id);
    }

    /// <summary>
    /// Loads the bytes with the given ID. Throws a <see cref="ArgumentException"/> if no
    /// such ID exists
    /// </summary>
    public byte[] Load(string id)
    {
        if (!_data.TryGetValue(id, out var blob))
        {
            throw new ArgumentException($"Could not find data with ID {id}");
        }

        return blob.Data;
    }

    /// <summary>
    /// Adds the metadata from the <paramref name="metadata"/> dictionary to the blob with the given <paramref name="id"/>
    /// </summary>
    public void AddMetadata(string id, Dictionary<string, string> metadata)
    {
        if (!_data.TryGetValue(id, out var blob))
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
        if (!_data.TryGetValue(id, out var blob))
        {
            throw new ArgumentException($"Could not find data with ID {id}");
        }

        return blob.Metadata.Clone();
    }

    /// <summary>
    /// Deletes the data stored under the given ID and returns true when some has been deleted. 
    /// </summary>
    public bool Delete(string id)
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
            
        return _data.TryRemove(id, out _);
    }

    /// <summary>
    /// Resets the data store (i.e. all stored data and metadata is deleted)
    /// </summary>
    public void Reset()
    {
        _data.Clear();
    }

    class InMemBlob
    {
        public InMemBlob(Dictionary<string, string> metadata, byte[] data)
        {
            Metadata = metadata?.Clone() ?? throw new ArgumentNullException(nameof(metadata));
            Data = data ?? throw new ArgumentNullException(nameof(data));
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