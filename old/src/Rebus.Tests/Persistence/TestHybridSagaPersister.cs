using System;
using NUnit.Framework;
using Ponder;
using Rebus.Persistence;
using Rebus.Persistence.InMemory;
using Rebus.Tests.Integration;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Persistence
{
    [TestFixture]
    public class TestHybridSagaPersister : FixtureBase
    {
        [Test]
        public void RequiresFallbackPersisterToBeConfigured()
        {
            Assert.Throws<ArgumentException>(() => new HybridSagaPersister(null));
        }

        [Test]
        public void UsesFallbackPersisterWhenNoCustomOnesAreConfigured()
        {
            // arrange
            var stringPath = Reflect.Path<ChunkOfData>(c => c.SomeString);
            var indexPaths = new[] {stringPath};

            var inMemorySagaPersister = new InMemorySagaPersister();
            var persister = new HybridSagaPersister(inMemorySagaPersister)
                .Add(new ThrowingSagaPersister());

            // act
            // assert
            var data = new ChunkOfData {SomeString = "Hello"};
            persister.Insert(data, indexPaths);

            var sagaCountAfterFirstInsert = inMemorySagaPersister.Count();

            var loadedData = persister.Find<ChunkOfData>(stringPath, "Hello");
            
            loadedData.ShouldNotBe(null);
            loadedData.SomeString.ShouldBe("Hello");

            loadedData.SomeString = "Hello again!";
            persister.Update(loadedData, indexPaths);

            var reloadedData = persister.Find<ChunkOfData>(stringPath, "Hello again!");
            
            reloadedData.ShouldNotBe(null);
            reloadedData.SomeString.ShouldBe("Hello again!");

            persister.Delete(reloadedData);
            var sagaCountAfterDelete = inMemorySagaPersister.Count();

            sagaCountAfterDelete.ShouldBe(0);
            sagaCountAfterFirstInsert.ShouldBe(1);
        }

        [Test]
        public void CanUseCustomizedPersisterWhenSettingSpecificInstance()
        {
            var fallbackPersister = new InMemorySagaPersister();
            var customPersister = new InMemorySagaPersister();

            var hybridPersister = new HybridSagaPersister(fallbackPersister)
                .Customize<AnotherChunkOfData>(customPersister);

            var data1 = new ChunkOfData {SomeString = "Hello"};
            hybridPersister.Insert(data1, new string[0]);

            var fallbackPersisterCountAfterFirstInsert = fallbackPersister.Count();
            var customPersisterCountAfterFirstInsert = customPersister.Count();

            var data2 = new AnotherChunkOfData {AnotherString = "Hello"};
            hybridPersister.Insert(data2, new string[0]);

            var fallbackPersisterCountAfterSecondInsert = fallbackPersister.Count();
            var customPersisterCountAfterSecondInsert = customPersister.Count();

            fallbackPersisterCountAfterFirstInsert.ShouldBe(1);
            customPersisterCountAfterFirstInsert.ShouldBe(0);

            fallbackPersisterCountAfterSecondInsert.ShouldBe(1);
            customPersisterCountAfterSecondInsert.ShouldBe(1);
        }

        [Test]
        public void CanUseCustomizedPersisterWhenSupplyingInstanceAndThenCustomizingWithTypeMapping()
        {
            var fallbackPersister = new InMemorySagaPersister();
            var customPersister = new InMemorySagaPersister();

            var hybridPersister = new HybridSagaPersister(fallbackPersister)
                .Add(customPersister)
                .Customize<AnotherChunkOfData, InMemorySagaPersister>();

            var data1 = new ChunkOfData {SomeString = "Hello"};
            hybridPersister.Insert(data1, new string[0]);

            var fallbackPersisterCountAfterFirstInsert = fallbackPersister.Count();
            var customPersisterCountAfterFirstInsert = customPersister.Count();

            var data2 = new AnotherChunkOfData {AnotherString = "Hello"};
            hybridPersister.Insert(data2, new string[0]);

            var fallbackPersisterCountAfterSecondInsert = fallbackPersister.Count();
            var customPersisterCountAfterSecondInsert = customPersister.Count();

            fallbackPersisterCountAfterFirstInsert.ShouldBe(1);
            customPersisterCountAfterFirstInsert.ShouldBe(0);

            fallbackPersisterCountAfterSecondInsert.ShouldBe(1);
            customPersisterCountAfterSecondInsert.ShouldBe(1);
        }

        class ChunkOfData : ISagaData
        {
            public ChunkOfData()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string SomeString { get; set; }
        }

        class AnotherChunkOfData : ISagaData
        {
            public AnotherChunkOfData()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string AnotherString { get; set; }
        }

        class ThrowingSagaPersister : IStoreSagaData
        {
            public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                throw new OmfgExceptionThisIsBad("don't call me!");
            }

            public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                throw new OmfgExceptionThisIsBad("don't call me!");
            }

            public void Delete(ISagaData sagaData)
            {
                throw new OmfgExceptionThisIsBad("don't call me!");
            }

            public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
            {
                throw new OmfgExceptionThisIsBad("don't call me!");
            }
        }
    }
}