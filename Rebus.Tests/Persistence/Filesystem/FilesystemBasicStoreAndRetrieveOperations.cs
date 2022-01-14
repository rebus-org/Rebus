using NUnit.Framework;
using Rebus.Tests.Contracts.Timeouts;

namespace Rebus.Tests.Persistence.Filesystem;

[TestFixture, Category(Categories.Filesystem)]
public class FilesystemBasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<FilesystemTimeoutManagerFactory>
{
}