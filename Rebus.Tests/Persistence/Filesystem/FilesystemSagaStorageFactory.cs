using System;
using System.IO;
using Rebus.Persistence.FileSystem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.Filesystem
{
    public class FilesystemSagaStorageFactory : ISagaStorageFactory
    {
        private string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Sagas{DateTime.Now:yyyyMMddHHmmssffff}");
        public ISagaStorage GetSagaStorage()
        {
            return new FilesystemSagaStorage(_basePath);
        }

        public void CleanUp()
        {
            Directory.Delete(_basePath, true);
        }
    }
}