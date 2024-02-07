using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Extensions;
#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Sagas
{
    /// <summary>
    /// Test fixture base class for verifying compliance with the <see cref="ISagaStorage"/> contract
    /// </summary>
    public abstract class BasicLoadAndSaveAndFindOperations<TFactory> : FixtureBase where TFactory : ISagaStorageFactory, new()
    {
        readonly IEnumerable<ISagaCorrelationProperty> _noCorrelationProperties = Enumerable.Empty<ISagaCorrelationProperty>();

        ISagaStorage _sagaStorage;
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();
            _sagaStorage = _factory.GetSagaStorage();
        }

        protected override void TearDown()
        {
            CleanUpDisposables();

            _factory.CleanUp();
        }

        [Test]
        public async Task IncludesTypeAsFindCriteria_CorrelationById()
        {
            var knownId = Guid.NewGuid();
            var correlationProperties = new[] { new TestCorrelationProperty(nameof(ISagaData.Id), typeof(Data1)) };
            await _sagaStorage.Insert(new Data1 { Id = knownId }, correlationProperties);

            var resultLookingForData1 = await _sagaStorage.Find(typeof(Data1), nameof(ISagaData.Id), knownId);
            var resultLookingForData2 = await _sagaStorage.Find(typeof(Data2), nameof(ISagaData.Id), knownId);

            Assert.That(resultLookingForData1, Is.Not.Null);
            Assert.That(resultLookingForData2, Is.Null);
        }

        [Test]
        public async Task IncludesTypeAsFindCriteria_CorrelationByCustomProperty()
        {
            const string knownCorrelationId = "known-correlation-property-id";
            var correlationProperties = new[] { new TestCorrelationProperty(nameof(Data1.CorrelationId), typeof(Data1)) };
            await _sagaStorage.Insert(new Data1 { Id = Guid.NewGuid(), CorrelationId = knownCorrelationId }, correlationProperties);

            var resultLookingForData1 = await _sagaStorage.Find(typeof(Data1), nameof(Data1.CorrelationId), knownCorrelationId);
            var resultLookingForData2 = await _sagaStorage.Find(typeof(Data2), nameof(Data1.CorrelationId), knownCorrelationId);

            Assert.That(resultLookingForData1, Is.Not.Null);
            Assert.That(resultLookingForData2, Is.Null);
        }

        [Test]
        [Description("Just verifies that querying for saga data with Id == '' (i.e. empty string) does not result in an error")]
        public async Task CanFindByEmptyStringPropertyValue_CorrelationById()
        {
            var result = await _sagaStorage.Find(typeof(Data1), nameof(Data1.Id), "");

            Assert.That(result, Is.Null);
        }

        [Test]
        [Description("Just verifies that querying for saga data with Id == NULL does not result in an error")]
        public async Task CanFindByNullPropertyValue_CorrelationById()
        {
            var result = await _sagaStorage.Find(typeof(Data1), nameof(Data1.Id), null);

            Assert.That(result, Is.Null);
        }

        [Test]
        [Description("Just verifies that querying for saga data with CorrelationId == NULL does not result in an error")]
        public async Task CanFindByNullPropertyValue_CorrelationByCustomProperty()
        {
            var result = await _sagaStorage.Find(typeof(Data1), nameof(Data1.CorrelationId), null);

            Assert.That(result, Is.Null);
        }

        class Data1 : SagaData { public string CorrelationId { get; set; } }
        class Data2 : SagaData { public string CorrelationId { get; set; } }

        [Test]
        public async Task CanSpecifySagaDataId()
        {
            var knownId = Guid.NewGuid();
            var correlationProperties = new[] { new TestCorrelationProperty(nameof(ISagaData.Id), typeof(Data1)) };
            await _sagaStorage.Insert(new DataWithCustomId { Id = knownId }, correlationProperties);

            var foundSagaData = await _sagaStorage.Find(typeof(DataWithCustomId), nameof(ISagaData.Id), knownId);

            Assert.That(foundSagaData, Is.Not.Null);
            Assert.That(foundSagaData.Id, Is.EqualTo(knownId));
        }

        class DataWithCustomId : SagaData { }

        [Test]
        public void ChecksRevisionOnFirstInsert()
        {
            var ex = Assert.Throws<AggregateException>(() =>
            {
                _sagaStorage
                    .Insert(new JustSomeSagaData
                    {
                        Id = Guid.NewGuid(),
                        Revision = 1
                    }, _noCorrelationProperties)
                    .Wait();
            });

            var invalidOperationException = ex.InnerExceptions.OfType<InvalidOperationException>().Single();
            Console.WriteLine(ex);

            Assert.That(invalidOperationException.Message, Does.Contain("revision must be 0 on first insert"));
        }

        public class JustSomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        [Test]
        public async Task DoesNotEnforceUniquenessOfCorrelationPropertyAcrossTypes()
        {
            var type1Property = new TestCorrelationProperty("CorrelationProperty", typeof(SagaDataType1));
            var type1Instance = new SagaDataType1
            {
                Id = Guid.NewGuid(),
                Revision = 0,
                CorrelationProperty = "hej"
            };
            var type2Property = new TestCorrelationProperty("CorrelationProperty", typeof(SagaDataType2));
            var type2Instance = new SagaDataType2
            {
                Id = Guid.NewGuid(),
                Revision = 0,
                CorrelationProperty = "hej"
            };

            await _sagaStorage.Insert(type1Instance, new[] { type1Property });
            await _sagaStorage.Insert(type2Instance, new[] { type2Property });
        }

        class SagaDataType1 : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationProperty { get; set; }
        }

        class SagaDataType2 : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationProperty { get; set; }
        }

        [Test]
        public async Task ThrowsIfIdHasNotBeenSet()
        {
            var sagaDataWithDefaultId = new AnotherSagaData { Id = Guid.Empty };

            var aggregateException = Assert.Throws<AggregateException>(() =>
            {
                _sagaStorage.Insert(sagaDataWithDefaultId, _noCorrelationProperties).Wait();
            });

            var baseException = aggregateException.GetBaseException();

            Assert.That(baseException, Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task GetsNullWhenNoInstanceMatches()
        {
            var data = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "whatever");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsNullWhenPropertyDoesNotExist()
        {
            var data = await _sagaStorage.Find(typeof(TestSagaData), "NonExistingCorrelationId", "whatever");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsNullWhenValueDoesNotExist()
        {
            await _sagaStorage.Insert(new TestSagaData { Id = Guid.NewGuid(), CorrelationId = "existing" }, _noCorrelationProperties);

            var data = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "non-existing");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsTheInstanceWhenCorrelationPropertyMatches()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" },
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var data = await _sagaStorage.Find(typeof(TestSagaData), "CorrelationId", "existing");

            Assert.That(data, Is.Not.Null);
            Assert.That(data.Id, Is.EqualTo(sagaId));
        }

        [Test]
        public async Task GetsNullWhenTheTypeDoesNotMatch()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" },
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var data = await _sagaStorage.Find(typeof(AnotherSagaData), "CorrelationId", "existing");

            Assert.That(data, Is.Null);
        }

        [Test]
        public async Task GetsTheInstanceWhenIdPropertyMatches()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData { Id = sagaId, CorrelationId = "existing" },
                CorrelationPropertiesFor<TestSagaData>(d => d.CorrelationId));

            var data = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(data, Is.Not.Null);
            Assert.That(data.Id, Is.EqualTo(sagaId));
        }

        [Test]
        public async Task NewlyInsertedSagaDataIsRevisionZero()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData
            {
                Id = sagaId,
                Data = "yes, den kender jeg"
            }, _noCorrelationProperties);

            var loadedSagaData = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaData.Revision, Is.EqualTo(0));
        }

        [Test]
        public async Task RevisionIsIncrementedOnEachUpdate()
        {
            var sagaId = Guid.NewGuid();

            var initialTransientInstance = new TestSagaData { Id = sagaId, Data = "yes, den kender jeg" };

            Assert.That(initialTransientInstance.Revision, Is.EqualTo(0));

            await _sagaStorage.Insert(initialTransientInstance, _noCorrelationProperties);
            var loadedSagaData0 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaData0.Revision, Is.EqualTo(0));
            Assert.That(initialTransientInstance.Revision, Is.EqualTo(0));

            await _sagaStorage.Update(loadedSagaData0, _noCorrelationProperties);
            var loadedSagaData1 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaData0.Revision, Is.EqualTo(1));
            Assert.That(loadedSagaData1.Revision, Is.EqualTo(1));

            await _sagaStorage.Update(loadedSagaData1, _noCorrelationProperties);
            var loadedSagaData2 = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaData1.Revision, Is.EqualTo(2));
            Assert.That(loadedSagaData2.Revision, Is.EqualTo(2));
        }

        [Test]
        public async Task RevisionIsIncrementedOnPassedInInstanceWhenDeleting()
        {
            var sagaId = Guid.NewGuid();

            var instance = new TestSagaData { Id = sagaId, Data = "yes, den kender jeg" };
            var initialRevision = instance.Revision;

            await _sagaStorage.Insert(instance, _noCorrelationProperties);
            var revisionAfterInsert = instance.Revision;

            await _sagaStorage.Update(instance, _noCorrelationProperties);
            var revisionAfterUpdate = instance.Revision;

            await _sagaStorage.Delete(instance);
            var revisionAfterDelete = instance.Revision;

            Assert.That(initialRevision, Is.EqualTo(0), "Expected initial revisio (before any saga persister actions) to be 0");
            Assert.That(revisionAfterInsert, Is.EqualTo(0), "Expected revision after first INSERT to be 0 because this is the first revision");
            Assert.That(revisionAfterUpdate, Is.EqualTo(1), "Expected revision after UPDATE to be 1 because is has now been saved as REV 1");
            Assert.That(revisionAfterDelete, Is.EqualTo(2), "Expceted revision after DELETE to be 2 because it's the best bet revision number to use even though it has most likely been deleted for good from the underlying storage");
        }

        [Test]
        public async Task CanDeleteSagaData()
        {
            var sagaId = Guid.NewGuid();

            await _sagaStorage.Insert(new TestSagaData
            {
                Id = sagaId,
                Data = "yes, den kender jeg"
            }, _noCorrelationProperties);

            var loadedSagaData = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaData, Is.Not.Null);

            await _sagaStorage.Delete(loadedSagaData);

            var loadedSagaDataAfterDelete = await _sagaStorage.Find(typeof(TestSagaData), "Id", sagaId);

            Assert.That(loadedSagaDataAfterDelete, Is.Null);
        }

        [Test]
        public async Task CorrelateByDifferentPropertyTypes()
        {
            var id = Guid.NewGuid();

            var guidCorrelationValue = Guid.NewGuid();
            var stringCorrelationValue = "hej";
            var dateTimeCorrelationValue = new DateTime(1979, 3, 19);
            var dateTimeOffsetCorrelationValue = new DateTimeOffset(1979, 3, 19, 20, 0, 0, TimeSpan.FromHours(2));
            var decimalCorrelationValue = 23M;
            var intCorrelationValue = 8;
            var boolCorrelationValue = true;
            var byteCorrelationValue = (byte)64;
            var shortCorrelationValue = (short)78;
            var longCorrelationValue = 2323232L;

            var data = new SagaDataWithVariousCorrelationProperties
            {
                Id = id,
                CorrelateByString = stringCorrelationValue,
                CorrelateByDateTime = dateTimeCorrelationValue,
                CorrelateByDateTimeOffset = dateTimeOffsetCorrelationValue,
                CorrelateByDecimal = decimalCorrelationValue,
                CorrelateByGuid = guidCorrelationValue,
                CorrelateByInt = intCorrelationValue,
                CorrelateByBool = boolCorrelationValue,
                CorrelateByByte = byteCorrelationValue,
                CorrelateByShort = shortCorrelationValue,
                CorrelateByLong = longCorrelationValue,
            };

            var correlationProperties = new[]
            {
                GetCorrelationProperty(d => d.CorrelateByString),

                GetCorrelationProperty(d => d.CorrelateByBool),
                GetCorrelationProperty(d => d.CorrelateByShort),
                GetCorrelationProperty(d => d.CorrelateByInt),
                GetCorrelationProperty(d => d.CorrelateByLong),
                GetCorrelationProperty(d => d.CorrelateByByte), 
                
                //GetCorrelationProperty(d => d.CorrelateByDecimal), 
                //GetCorrelationProperty(d => d.CorrelateByDateTime), 
                //GetCorrelationProperty(d => d.CorrelateByDateTimeOffset), 
                GetCorrelationProperty(d => d.CorrelateByGuid),
            };

            await _sagaStorage.Insert(data, correlationProperties);

            var dataByString = await Find(stringCorrelationValue, d => d.CorrelateByString);
            var dataByInt = await Find(intCorrelationValue, d => d.CorrelateByInt);
            var dataByGuid = await Find(guidCorrelationValue, d => d.CorrelateByGuid);

            var dataByBool = await Find(boolCorrelationValue, d => d.CorrelateByBool);
            var dataByByte = await Find(byteCorrelationValue, d => d.CorrelateByByte);
            var dataByShort = await Find(shortCorrelationValue, d => d.CorrelateByShort);
            var dataByLong = await Find(longCorrelationValue, d => d.CorrelateByLong);

            Assert.That(dataByString.Id, Is.EqualTo(id));
            Assert.That(dataByInt.Id, Is.EqualTo(id));
            Assert.That(dataByGuid.Id, Is.EqualTo(id));
            Assert.That(dataByBool.Id, Is.EqualTo(id));
            Assert.That(dataByShort.Id, Is.EqualTo(id));
            Assert.That(dataByLong.Id, Is.EqualTo(id));
            Assert.That(dataByByte.Id, Is.EqualTo(id));
        }

        async Task<ISagaData> Find(object value, Expression<Func<SagaDataWithVariousCorrelationProperties, object>> expression)
        {
            return await _sagaStorage.Find(typeof(SagaDataWithVariousCorrelationProperties), Reflect.Path(expression), value);
        }

        TestCorrelationProperty GetCorrelationProperty(Expression<Func<SagaDataWithVariousCorrelationProperties, object>> expression)
        {
            return new TestCorrelationProperty(Reflect.Path(expression), typeof(SagaDataWithVariousCorrelationProperties));
        }

        class SagaDataWithVariousCorrelationProperties : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public string CorrelateByString { get; set; }
            public Guid CorrelateByGuid { get; set; }

            public bool CorrelateByBool { get; set; }
            public short CorrelateByShort { get; set; }
            public int CorrelateByInt { get; set; }
            public long CorrelateByLong { get; set; }

            public decimal CorrelateByDecimal { get; set; }
            public DateTime CorrelateByDateTime { get; set; }
            public DateTimeOffset CorrelateByDateTimeOffset { get; set; }
            public byte CorrelateByByte { get; set; }
        }

        class TestSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationId { get; set; }
            public string Data { get; set; }
        }

        class AnotherSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        IEnumerable<ISagaCorrelationProperty> CorrelationPropertiesFor<TSagaData>(params Expression<Func<TSagaData, object>>[] properties)
        {
            return properties
                .Select(Reflect.Path)
                .Select(propertyName => new TestCorrelationProperty(propertyName, typeof(TSagaData)));
        }
    }
}