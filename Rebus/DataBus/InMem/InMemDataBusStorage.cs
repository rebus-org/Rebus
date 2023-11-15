using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Time;
// ReSharper disable EmptyGeneralCatchClause
#pragma warning disable 1998

namespace Rebus.DataBus.InMem;

class InMemDataBusStorage : IDataBusStorage, IDataBusStorageManagement
{
    readonly InMemDataStore _dataStore;
    readonly IRebusTime _rebusTime;

    public InMemDataBusStorage(InMemDataStore dataStore, IRebusTime rebusTime)
    {
        _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
    }

    public async Task Save(string id, Stream source, Dictionary<string, string> metadata = null)
    {
        using var destination = new MemoryStream();
        
        await source.CopyToAsync(destination);
        
        var bytes = destination.ToArray();

        var metadataToWrite = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>())
        {
            [MetadataKeys.SaveTime] = _rebusTime.Now.ToString("O"),
            [MetadataKeys.Length] = bytes.Length.ToString()
        };

        _dataStore.Save(id, bytes, metadataToWrite);
    }

    public async Task<Stream> Read(string id)
    {
        var now = _rebusTime.Now;

        var metadata = new Dictionary<string, string>
        {
            {MetadataKeys.ReadTime, now.ToString("O") }
        };

        _dataStore.AddMetadata(id, metadata);

        var source = new MemoryStream(_dataStore.Load(id));

        return source;
    }

    public async Task<Dictionary<string, string>> ReadMetadata(string id) => _dataStore.LoadMetadata(id);

    public async Task Delete(string id) => _dataStore.Delete(id);

    public IEnumerable<string> Query(TimeRange readTime = null, TimeRange saveTime = null)
    {
        return _dataStore
            .AttachmentIds
            .Where(id =>
            {
                var metadata = _dataStore.LoadMetadata(id);

                return DataBusStorageQuery.IsSatisfied(metadata, readTime, saveTime);
            });
    }
}