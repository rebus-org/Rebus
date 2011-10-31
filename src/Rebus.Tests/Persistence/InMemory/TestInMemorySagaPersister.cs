using System;
using NUnit.Framework;
using Ponder;
using Rebus.Persistence.InMemory;

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
            var someSagaData = new SomeSagaData{Id=someId};

            persister.Save(someSagaData, new[]{""});
            var sagaData = persister.Find("Id", someId.ToString());

            Assert.AreSame(someSagaData, sagaData);
        }

        [Test]
        public void CanSaveAndFindSagasByDeepPath()
        {
            persister.UseIndex(new[] {Reflect.Path<SomeSagaData>(d => d.AggregatedObject.SomeRandomValue)});
            var someSagaData = new SomeSagaData{AggregatedObject = new SomeAggregatedObject{SomeRandomValue = "whooHAAA!"}};

            persister.Save(someSagaData, new[] { "" });
            var sagaData = persister.Find(Reflect.Path<SomeSagaData>(d => d.AggregatedObject.SomeRandomValue), "whooHAAA!");

            Assert.AreSame(someSagaData, sagaData);
        }

        [Test]
        public void GetsNullIfSagaCannotBeFound()
        {
            var sagaData = new SomeSagaData {AggregatedObject = new SomeAggregatedObject {SomeRandomValue = "whooHAAA!"}};
            persister.Save(sagaData, new[] {""});

            Assert.IsNull(persister.Find(Reflect.Path<SomeSagaData>(d => d.AggregatedObject.SomeRandomValue), "NO MATCH"));
            Assert.IsNull(persister.Find("Invalid.Path.To.Nothing", "whooHAAA!"));
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