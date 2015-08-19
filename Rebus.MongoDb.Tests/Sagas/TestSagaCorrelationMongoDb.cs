using NUnit.Framework;
using Rebus.Tests.Integration;

namespace Rebus.MongoDb.Tests.Sagas
{
    [TestFixture, Category(MongoTestHelper.TestCategory)]
    public class TestSagaCorrelationMongoDb : TestSagaCorrelation<TestMongoDbSagaStorage> { }
}