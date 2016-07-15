using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.RavenDb.Tests.Sagas
{
    [TestFixture, Category(TestCategory.RavenDb)]
    public class RavenDbBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<RavenDbSagaStorageFactory> { }
}