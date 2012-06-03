using System;
using NUnit.Framework;
using Rebus.MongoDb;
using Shouldly;

namespace Rebus.Tests.Persistence.MongoDb
{
    [TestFixture, Category(TestCategories.Mongo)]
    public class TestMongoDbSagaPersister : MongoDbFixtureBase
    {
        MongoDbSagaPersister persister;

        protected override void DoSetUp()
        {
            persister = new MongoDbSagaPersister(ConnectionString);

            DropCollection("sagas_FirstSagaData");
            DropCollection("sagas_SecondSagaData");
            DropCollection("second_saga_datas");
        }

        [Test]
        public void ThrowsIfSagaTypeIsUnknownAndNotAllowedToComeUpWithNamesAutomatically()
        {
            // arrange

            // act

            // assert
            Assert.Throws<InvalidOperationException>(() => persister.Insert(new UnknownSagaData(), new string[0]));
        }

        class UnknownSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        [Test]
        public void CanComeUpWithCollectionNameAutomatically()
        {
            // arrange
            persister.AllowAutomaticSagaCollectionNames();

            GetCollectionNames().ShouldNotContain("sagas_FirstSagaData");
            GetCollectionNames().ShouldNotContain("sagas_SecondSagaData");

            // act
            persister.Insert(new FirstSagaData(), new string[0]);
            persister.Insert(new SecondSagaData(), new string[0]);

            // assert
            GetCollectionNames().ShouldContain("sagas_FirstSagaData");
            GetCollectionNames().ShouldContain("sagas_SecondSagaData");
        }

        class FirstSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        class SecondSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        [Test]
        public void PrefersExplicitlyConfiguredCollectionName()
        {
            // arrange
            persister
                .SetCollectionName<SecondSagaData>("second_saga_datas")
                .AllowAutomaticSagaCollectionNames();

            GetCollectionNames().ShouldNotContain("sagas_FirstSagaData");
            GetCollectionNames().ShouldNotContain("second_saga_datas");

            // act
            persister.Insert(new FirstSagaData(), new string[0]);
            persister.Insert(new SecondSagaData(), new string[0]);

            // assert
            GetCollectionNames().ShouldContain("sagas_FirstSagaData");
            GetCollectionNames().ShouldContain("second_saga_datas");
        }
    }
}