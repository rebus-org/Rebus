using System;
using System.IO;
using Rebus.DataBus;
using Rebus.DataBus.FileSystem;
using Rebus.Logging;

namespace Rebus.Tests.Contracts.DataBus.Factories
{
    public class FileSystemDataBusStorageFactory : IDataBusStorageFactory
    {
        static readonly string DirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "databus");

        public FileSystemDataBusStorageFactory()
        {
            CleanUpDirectory();
        }

        public IDataBusStorage Create()
        {
            var fileSystemDataBusStorage = new FileSystemDataBusStorage(DirectoryPath, new ConsoleLoggerFactory(false));
            fileSystemDataBusStorage.Initialize();
            return fileSystemDataBusStorage;
        }

        public void CleanUp()
        {
            CleanUpDirectory();
        }

        static void CleanUpDirectory()
        {
            if (!Directory.Exists(DirectoryPath)) return;

            Console.WriteLine($"Removing directory '{DirectoryPath}'");
            Directory.Delete(DirectoryPath, true);
        }
    }
}