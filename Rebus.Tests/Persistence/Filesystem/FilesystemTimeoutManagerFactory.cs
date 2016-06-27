using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Persistence.FileSystem;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Tests.Persistence.SqlServer;
using Rebus.Timeouts;

namespace Rebus.Tests.Persistence.Filesystem
{
    [TestFixture, Category(Categories.Filesystem)]
    public class FilesystemBasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<FilesystemTimeoutManagerFactory>
    {
    }
    public class FilesystemTimeoutManagerFactory : ITimeoutManagerFactory
    {
        private string _basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,$"Timeouts{DateTime.Now:yyyyMMddHHmmssffff}");
        public ITimeoutManager Create()
        {
            return new FilesystemTimeoutManager(_basePath, new ConsoleLoggerFactory(false));
        }

        public void Cleanup()
        {
            try
            {
                Directory.Delete(_basePath, true);
            }
            catch(IOException ex) { }
        }

        public string GetDebugInfo()
        {
            return "could not provide debug info for this particular timeout manager.... implement if needed :)";
        }
    }
}
