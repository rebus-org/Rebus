using Rebus.Tests.Contracts.Timeouts;
using Xunit;

namespace Rebus.Tests.Persistence.Filesystem
{
    [Trait("Category", Categories.Filesystem)]
    public class FilesystemBasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<FilesystemTimeoutManagerFactory>
    {
    }
}