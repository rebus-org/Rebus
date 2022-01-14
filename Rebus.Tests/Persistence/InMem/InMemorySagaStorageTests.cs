using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Persistence.InMem;
using Rebus.Sagas;

namespace Rebus.Tests.Persistence.InMem;

[TestFixture]
public class InMemorySagaStorageTests
{
    private InMemorySagaStorage _inMemorySagaStorage;
    private IEnumerable<ISagaCorrelationProperty> _correlationProperties;

    [SetUp]
    public void SetUp()
    {
        _inMemorySagaStorage = new InMemorySagaStorage();
        _correlationProperties = new TestSaga().GetCorrelationProperties();
    }

    [Test]
    public void Instances_Empty_ReturnsEmpty()
    {
        Assert.That(_inMemorySagaStorage.Instances, Is.Empty);
    }

    [Test]
    public async Task Instances_WithData_ReturnsData()
    {
        await _inMemorySagaStorage.Insert(new TestSagaData {RequestId = "1"}, _correlationProperties);
        await _inMemorySagaStorage.Insert(new TestSagaData {RequestId = "2"}, _correlationProperties);

        var instances = _inMemorySagaStorage.Instances.Cast<TestSagaData>().OrderBy(d => d.RequestId).ToList();

        Assert.That(instances, Has.Count.EqualTo(2));
        Assert.That(instances[0].RequestId, Is.EqualTo("1"));
        Assert.That(instances[1].RequestId, Is.EqualTo("2"));
    }
        
    [Test]
    public void Reset_Empty_DoesNothing()
    {
        Assert.That(() => _inMemorySagaStorage.Reset(), Throws.Nothing);
    }

    [Test]
    public async Task Reset_WithData_ClearsData()
    {
        var testSagaData = new TestSagaData {RequestId = "1"};
        await _inMemorySagaStorage.Insert(testSagaData, _correlationProperties);
            
        Assert.That(_inMemorySagaStorage.Instances, Is.Not.Empty);
            
        _inMemorySagaStorage.Reset();
        Assert.That(_inMemorySagaStorage.Instances, Is.Empty);
    }

    [Test]
    public async Task Reset_WithData_RaisesDeletedForRemovedData()
    {
        await _inMemorySagaStorage.Insert(new TestSagaData {RequestId = "1"}, _correlationProperties);
        await _inMemorySagaStorage.Insert(new TestSagaData {RequestId = "2"}, _correlationProperties);

        var requestIds = new List<string>();
        _inMemorySagaStorage.Deleted += d => requestIds.Add(((TestSagaData) d).RequestId);
        _inMemorySagaStorage.Reset();

        Assert.That(requestIds, Is.EquivalentTo(new[] {"1", "2"}));
    }

    private class TestSagaData : SagaData
    {
        public string RequestId { get; set; }

        public TestSagaData()
        {
            Id = Guid.NewGuid();
        }
    }

    private class TestSaga : Saga<TestSagaData>
    {
        protected override void CorrelateMessages(ICorrelationConfig<TestSagaData> config)
        {
            config.Correlate<string>(m => m, d => d.RequestId);
        }
    }
}