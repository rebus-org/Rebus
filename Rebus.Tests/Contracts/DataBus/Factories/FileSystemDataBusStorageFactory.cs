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
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, true);
            }
        }

        public IDataBusStorage Create()
        {
            var fileSystemDataBusStorage = new FileSystemDataBusStorage(DirectoryPath, new ConsoleLoggerFactory(false));
            fileSystemDataBusStorage.Initialize();
            return fileSystemDataBusStorage;
        }

        public void CleanUp()
        {
            Directory.Delete(DirectoryPath, true);
        }
    }
}