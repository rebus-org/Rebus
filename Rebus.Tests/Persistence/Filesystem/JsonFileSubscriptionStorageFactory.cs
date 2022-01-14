using System;
using System.IO;
using Rebus.Persistence.FileSystem;
using Rebus.Subscriptions;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Subscriptions;

namespace Rebus.Tests.Persistence.Filesystem;

public class JsonFileSubscriptionStorageFactory : ISubscriptionStorageFactory
{
    readonly string _xmlDataFilePath = Path.Combine(TestConfig.DirectoryPath(), "subscriptions.json");

    public ISubscriptionStorage Create()
    {
        CleanupOldDataFile();

        Console.WriteLine("Using JSON file at {0}", _xmlDataFilePath);

        var storage = new JsonFileSubscriptionStorage(_xmlDataFilePath);

        return storage;
    }

    public void Cleanup()
    {
        CleanupOldDataFile();
    }

    void CleanupOldDataFile()
    {
        if (File.Exists(_xmlDataFilePath)) File.Delete(_xmlDataFilePath);
    }
}