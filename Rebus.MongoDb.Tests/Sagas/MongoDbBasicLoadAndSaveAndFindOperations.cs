using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.MongoDb.Tests.Sagas
{
    [TestFixture, Category(MongoTestHelper.TestCategory)]
    public class MongoDbBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<TestMongoDbSagaStorage> { }
}