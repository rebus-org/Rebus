using System;
using NUnit.Framework;
using Ponder;
using Rebus.Persistence.InMemory;
using Shouldly;

namespace Rebus.Tests.Persistence.InMemory
{
    [TestFixture]
    public class TestInMemorySagaPersister : FixtureBase
    {
        InMemorySagaPersister persister;

        protected override void DoSetUp()
        {
            persister = new InMemorySagaPersister();
        }

        [Test]
        public void CanSaveAndFindSagasById()
        {
            persister.UseIndex(new[] {"Id"});
            var someId = Guid.NewGuid();
            var mySagaData = new SomeSagaData{Id=someId};

            persister.Save(mySagaData, new[]{""});
            var storedSagaData = persister.Find("Id", someId.ToString());

            storedSagaData.ShouldBeSameAs(mySagaData);
        }

        [Test]
        public void CanSaveAndFindSagasByDeepPath()
        {
            persister.UseIndex(new[] {Reflect.Path<SomeSagaData>(d => d.AggregatedObject.SomeRandomValue)});
            var mySagaData = new SomeSagaData{AggregatedObject = new SomeAggregatedObject{SomeRandomValue = "whooHAAA!"}};

            persister.Save(mySagaData, new[] { "" });
            var storedSagaData = persister.Find(Reflect.Path<SomeSagaData>(d => d.AggregatedObject.SomeRandomValue), "whooHAAA!");

            storedSagaData.ShouldBeSameAs(mySagaData);
        }

        [Test]
        public void GetsNullIfSagaCannotBeFound()
        {
            var sagaData = new SomeSagaData {AggregatedObject = new SomeAggregatedObject {SomeRandomValue = "whooHAAA!"}};
            persister.Save(sagaData, new[] {""});

            persister.Find(Reflect.Path<SomeSagaData>(d => d.AggregatedObject.SomeRandomValue), "NO MATCH")
                .ShouldBe(null);

            persister.Find("Invalid.Path.To.Nothing", "whooHAAA!")
                .ShouldBe(null);
        }

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }

            public SomeAggregatedObject AggregatedObject { get; set; }
        }

        class SomeAggregatedObject
        {
            public string SomeRandomValue { get; set; }
        }
    }
}