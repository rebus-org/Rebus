using System;
using System.IO;
using Rebus.DataBus;
using Rebus.DataBus.FileSystem;
using Rebus.Logging;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.DataBus;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Tests.Time;

namespace Rebus.Tests.DataBus.FileSystem;

public class FileSystemDataBusStorageFactory : IDataBusStorageFactory
{
    static readonly string DirectoryPath = Path.Combine(TestConfig.DirectoryPath(), "databus");

    readonly FakeRebusTime _fakeRebusTime = new FakeRebusTime();

    public FileSystemDataBusStorageFactory()
    {
        CleanUpDirectory();
    }

    public IDataBusStorage Create()
    {
        var fileSystemDataBusStorage = new FileSystemDataBusStorage(DirectoryPath, new ConsoleLoggerFactory(false), _fakeRebusTime);
        fileSystemDataBusStorage.Initialize();
        return fileSystemDataBusStorage;
    }

    public void CleanUp()
    {
        CleanUpDirectory();

        _fakeRebusTime.Reset();
    }

    public void FakeIt(DateTimeOffset fakeTime) => _fakeRebusTime.FakeIt(fakeTime);

    static void CleanUpDirectory()
    {
        if (!Directory.Exists(DirectoryPath)) return;

        Console.WriteLine($"Removing directory '{DirectoryPath}'");

        DeleteHelper.DeleteDirectory(DirectoryPath);
    }
}