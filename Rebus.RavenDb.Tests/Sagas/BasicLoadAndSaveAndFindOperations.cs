using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.RavenDb.Tests.Sagas
{
    [TestFixture]
    public class BasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<RavenDbSagaStorageFactory> { }
}