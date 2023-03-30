using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Snapshots;

[TestFixture]
public class FileSystemSnapshotStorageContractTests : SagaSnapshotStorageTest<FileSystemSagaSnapshotStorageFactory>
{
}