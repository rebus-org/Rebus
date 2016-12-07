using Rebus.Tests.Contracts.Sagas;
using Xunit;

namespace Rebus.Tests.Persistence.Filesystem
{
    [Trait("Category", Categories.Filesystem)]
    public class FilesystemSagaStorageBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<FilesystemSagaStorageFactory> { }
}