using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Persistence.Filesystem;

[TestFixture, Category(Categories.Filesystem)]
public class FilesystemSagaStorageBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<FilesystemSagaStorageFactory> { }