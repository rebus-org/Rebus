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
#if NET45
        readonly string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Sagas{DateTime.Now:yyyyMMddHHmmssffff}");
#elif NETSTANDARD1_6
        readonly string _basePath = Path.Combine(AppContext.BaseDirectory, $"Sagas{DateTime.Now:yyyyMMddHHmmssffff}");
#endif

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