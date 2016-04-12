using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.RavenDb.Tests.Sagas.Models;
using Rebus.Sagas;
using Rebus.Tests;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.RavenDb.Tests.Sagas
{
    [TestFixture]
    public class TestRavenDbSagaStorage : FixtureBase
    {
        RavenDbSagaStorageFactory _factory;
        ISagaStorage _sagaStorage;
        TestCorrelationProperty[] _correlationProperties;

        protected override void SetUp()
        {
            _factory = new RavenDbSagaStorageFactory();
            _sagaStorage = _factory.GetSagaStorage();

            _correlationProperties = new[] {
                new TestCorrelationProperty(nameof(BasicSagaData.Id), typeof(BasicSagaData)),
                new TestCorrelationProperty(nameof(BasicSagaData.StringField), typeof(BasicSagaData)),
                new TestCorrelationProperty(nameof(BasicSagaData.IntegerField), typeof(BasicSagaData))
            };
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Test]
        public async Task CanLoadAndSaveSagaData()
        {
            var initialSagaData = new BasicSagaData
            {
                Id = Guid.NewGuid(),
                IntegerField = 77,
                StringField = "Springfield"
            };

            await _sagaStorage.Insert(initialSagaData, _correlationProperties);

            var match = await _sagaStorage.Find(typeof(BasicSagaData), nameof(BasicSagaData.StringField), "Springfield");

            Assert.That(match, Is.Not.Null);
            Assert.That(match.Id, Is.EqualTo(initialSagaData.Id));
        }

        [Test]
        public async Task ThrowsOnDuplicateCorrelationProperty()
        {
            var firstInstance = new BasicSagaData
            {
                Id = Guid.NewGuid(),
                IntegerField = 55,
                StringField = "hej",
            };

            var secondInstance = new BasicSagaData
            {
                Id = Guid.NewGuid(),
                IntegerField = 55,
                StringField = "hej igen min ven",
            };

            await _sagaStorage.Insert(firstInstance, _correlationProperties);

            var exception = Assert.Throws<AggregateException>(() =>
            {
                _sagaStorage.Insert(secondInstance, _correlationProperties).Wait();
            });

            Console.WriteLine(exception);
        }
    }
}