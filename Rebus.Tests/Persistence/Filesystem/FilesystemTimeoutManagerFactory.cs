using System;
using System.IO;
using Rebus.Logging;
using Rebus.Persistence.FileSystem;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Timeouts;

namespace Rebus.Tests.Persistence.Filesystem
{
    public class FilesystemTimeoutManagerFactory : ITimeoutManagerFactory
    {
#if NET45
        readonly string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Timeouts{DateTime.Now:yyyyMMddHHmmssffff}");
#elif NETSTANDARD1_6
        readonly string _basePath = Path.Combine(AppContext.BaseDirectory, $"Timeouts{DateTime.Now:yyyyMMddHHmmssffff}");
#endif

        public ITimeoutManager Create()
        {
            return new FileSystemTimeoutManager(_basePath, new ConsoleLoggerFactory(false));
        }

        public void Cleanup()
        {
            DeleteHelper.DeleteDirectory(_basePath);
        }

        public string GetDebugInfo()
        {
            return "could not provide debug info for this particular timeout manager.... implement if needed :)";
        }
    }
}
