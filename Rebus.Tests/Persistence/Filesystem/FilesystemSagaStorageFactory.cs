using System;
using System.IO;
using Rebus.Logging;
using Rebus.Persistence.FileSystem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;
using Rebus.Tests.Contracts.Utilities;

namespace Rebus.Tests.Persistence.Filesystem
{
    public class FilesystemSagaStorageFactory : ISagaStorageFactory
    {
        readonly string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Sagas{DateTime.Now:yyyyMMddHHmmssffff}");

        public ISagaStorage GetSagaStorage()
        {
            return new FileSystemSagaStorage(_basePath, new ConsoleLoggerFactory(false));
        }

        public void CleanUp()
        {
            DeleteHelper.DeleteDirectory(_basePath);
        }
    }
}