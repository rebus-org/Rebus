using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Exceptions;
using Rebus.Sagas;
using Rebus.Tests;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.RavenDb.Tests.Sagas
{
    [TestFixture]
    public class TestRavenDbSagaDataConcurrencyOperations : FixtureBase
    {
        RavenDbSagaStorageFactory _factory;
        ISagaStorage _storage;

        protected override void SetUp()
        {
            _factory = new RavenDbSagaStorageFactory();
            _storage = _factory.GetSagaStorage();
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Test]
        public async Task CannotInsertSameSagaDataTwice()
        {
            var correlationProperties = new ISagaCorrelationProperty[]
            {
                new TestCorrelationProperty(nameof(JustSomeSagaData.CorrelationProperty1), typeof(JustSomeSagaData)),
                new TestCorrelationProperty(nameof(JustSomeSagaData.CorrelationProperty2), typeof(JustSomeSagaData)),
            };

            var sagaDataId = Guid.NewGuid();
            var sagaData = new JustSomeSagaData { Id = sagaDataId };
            await _storage.Insert(sagaData, correlationProperties);

            var aggregateException = Assert.Throws<AggregateException>(() =>
            {
                _storage.Insert(sagaData, correlationProperties).Wait();
            });

            var baseException = aggregateException.GetBaseException();

            Console.WriteLine(baseException);

            Assert.That(baseException, Is.TypeOf<ConcurrencyException>());
        }

        [Test]
        public async Task CannotUpdateSagaDataInParallel()
        {
            var correlationProperties = new ISagaCorrelationProperty[]
            {
                new TestCorrelationProperty(nameof(JustSomeSagaData.CorrelationProperty1), typeof(JustSomeSagaData)),
                new TestCorrelationProperty(nameof(JustSomeSagaData.CorrelationProperty2), typeof(JustSomeSagaData)),
            };

            var sagaData = new JustSomeSagaData
            {
                Id = Guid.NewGuid(),
                CorrelationProperty1 = "c1",
                CorrelationProperty2 = "c2"
            };

            await _storage.Insert(sagaData, correlationProperties);

            var data1 = (JustSomeSagaData)await _storage.Find(typeof(JustSomeSagaData), nameof(JustSomeSagaData.CorrelationProperty1), "c1");
            var data2 = (JustSomeSagaData)await _storage.Find(typeof(JustSomeSagaData), nameof(JustSomeSagaData.CorrelationProperty2), "c2");

            data1.Counter++;
            data2.Counter++;

            await _storage.Update(data1, correlationProperties);

            var aggregateException = Assert.Throws<AggregateException>(() =>
            {
                _storage.Update(data2, correlationProperties).Wait();
            });

            var baseException = aggregateException.GetBaseException();

            Console.WriteLine(baseException);

            Assert.That(baseException, Is.TypeOf<ConcurrencyException>());
        }

        class JustSomeSagaData : SagaData
        {
            public string CorrelationProperty1 { get; set; }
            public string CorrelationProperty2 { get; set; }

            public int Counter { get; set; }
        }
    }
}