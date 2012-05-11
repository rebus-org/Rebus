using System;
using System.Transactions;
using NUnit.Framework;
using Rebus.Tests.Persistence.Sagas.Factories;
using Shouldly;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(SqlServerSagaPersisterFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    public class TestSagaPersistersEmployingTransactionalConcurrency<TFactory> : TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
    {
        [Test]
        public void SavingSagaDataIsTransactional()
        {
            var sagaDataId = Guid.NewGuid();
            var sagaData = new MySagaData { Id = sagaDataId, SomeField = "some value" };

            using (var tx = new TransactionScope())
            {
                Persister.Save(sagaData, new string[0]);

                // no complete!
            }

            EnterAFakeMessageContext();
            var mySagaData = Persister.Find<MySagaData>("Id", sagaDataId);
            mySagaData.ShouldBe(null);
            ReturnToOriginalMessageContext();
        }
    }
}