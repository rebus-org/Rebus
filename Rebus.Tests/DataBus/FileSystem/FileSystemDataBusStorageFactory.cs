using System;
using System.IO;
using Rebus.DataBus;
using Rebus.DataBus.FileSystem;
using Rebus.Logging;
using Rebus.Tests.Contracts.DataBus;
using Rebus.Tests.Contracts.Utilities;

namespace Rebus.Tests.DataBus.FileSystem
{
    public class FileSystemDataBusStorageFactory : IDataBusStorageFactory
    {
#if NET45
        static readonly string DirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "databus");
#elif NETSTANDARD1_6
        static readonly string DirectoryPath = Path.Combine(AppContext.BaseDirectory, "databus");
#endif


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

            DeleteHelper.DeleteDirectory(DirectoryPath);

        }
    }
}