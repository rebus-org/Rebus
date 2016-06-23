using System;
using System.IO;
using Rebus.Persistence.FileSystem;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;
using Rebus.Tests.Transport.FileSystem;

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
            var success = false;
            while (!success)
            {
                try
                {
                    Directory.Delete(_basePath, true);
                    success = true;
                }
                catch (IOException ex)
                {
                    System.Threading.Thread.Sleep(TimeSpan.FromTicks(1));
                }
            }
        }
    }
}