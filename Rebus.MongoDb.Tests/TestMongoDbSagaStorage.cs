using NUnit.Framework;
using Rebus.Tests.Contracts.Sagas;
using Rebus2.Sagas;

namespace Rebus.MongoDb.Tests
{
    [TestFixture]
    public class TestMongoDbSagaStorage : ISagaStorageFactory
    {
        public class BasicOperations : BasicOperations<TestMongoDbSagaStorage> { }

        public ISagaStorage GetSagaStorage()
        {
            return new MongoDbSagaStorage();
        }

        public void Cleanup()
        {
            
        }
    }
}
