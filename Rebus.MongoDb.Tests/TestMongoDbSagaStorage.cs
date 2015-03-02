using NUnit.Framework;

namespace Rebus.MongoDb.Tests
{
    [TestFixture]
    public class TestMongoDbSagaStorage
    {
        public class BasicOperations : global::Tests.Contracts.Sagas.BasicOperations { }
    }
}
