using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Rebus.Auditing.Sagas;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Persistence.FileSystem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Tests.Contracts.Sagas;
using Rebus.Tests.Contracts.Utilities;

namespace Rebus.Tests.Snapshots;

public class FileSystemSagaSnapshotStorageFactory : ISagaSnapshotStorageFactory
{
    readonly ConcurrentStack<IDisposable> _disposables = new();
    readonly string _testDirectoryPath;

    public FileSystemSagaSnapshotStorageFactory()
    {
        _testDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString("N"));

        _disposables.Push(new DisposableCallback(() =>
        {
            if (!Directory.Exists(_testDirectoryPath)) return;
            Console.WriteLine($"Deleting directory {_testDirectoryPath}");
            Directory.Delete(_testDirectoryPath, true);
        }));
    }

    public ISagaSnapshotStorage Create() => new FileSystemSagaSnapshotStorage(_testDirectoryPath, new ConsoleLoggerFactory(false));

    public void Dispose() => _disposables.Dispose();

    public IEnumerable<SagaDataSnapshot> GetAllSnapshots()
    {
        return Directory.GetFiles(_testDirectoryPath, "*.json")
            .Select(Parse)
            .ToList();
    }

    static SagaDataSnapshot Parse(string filePath)
    {
        var json = File.ReadAllText(filePath);

        try
        {
            var jObject = JObject.Parse(json);
            var metadata = jObject[nameof(Snapshot.Metadata)].ToObject<Dictionary<string, string>>();

            var sagaDataTypeName = metadata.GetValueOrNull(SagaAuditingMetadataKeys.SagaDataType)
                                   ?? throw new KeyNotFoundException($"Could not find saga data metadata with key '{SagaAuditingMetadataKeys.SagaDataType}'");

            var sagaDataType = Type.GetType(sagaDataTypeName)
                               ?? throw new ArgumentException($"Could not find .NET type with name {sagaDataTypeName}");

            var sagaDataObject = jObject[nameof(Snapshot.Data)].ToObject(sagaDataType);
            var sagaData = sagaDataObject as ISagaData
                           ?? throw new ArgumentException($"The deserialized saga instance {sagaDataObject} was not ISagaData");

            return new SagaDataSnapshot { Metadata = metadata, SagaData = sagaData };
        }
        catch (Exception exception)
        {
            throw new FormatException($@"Could not deserialize JSON

{json}

into saga snapshot", exception);
        }
    }

    class Snapshot
    {
        public Dictionary<string, string> Metadata { get; set; }
        public ISagaData Data { get; set; }
    }
}