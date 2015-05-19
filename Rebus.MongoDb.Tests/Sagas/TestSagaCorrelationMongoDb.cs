using NUnit.Framework;
using Rebus.Tests.Integration;

namespace Rebus.MongoDb.Tests.Sagas
{
    [TestFixture]
    public class TestSagaCorrelationMongoDb : TestSagaCorrelation<TestMongoDbSagaStorage> { }
}