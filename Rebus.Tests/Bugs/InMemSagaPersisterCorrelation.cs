using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Persistence.InMem;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.Tests.Bugs;

[TestFixture]
public class InMemSagaPersisterCorrelationPropertyUniqueness : FixtureBase
{
    [Test]
    public void DoesNotEnforceUniquenessAcrossTypes ()
    {
        var sagaStorage = new InMemorySagaStorage();

        var recycledId = "recycled-id";

        var instanceOfType1 = new Type1 {Id = Guid.NewGuid(), Revision = 0, CorrelationId = recycledId};
        var instanceOfType2 = new Type2 { Id = Guid.NewGuid(), Revision = 0, CorrelationId = recycledId };

        sagaStorage.Insert(instanceOfType1, For(typeof(Type1))).Wait();
        sagaStorage.Insert(instanceOfType2, For(typeof(Type2))).Wait();
    }

    static IEnumerable<ISagaCorrelationProperty> For(Type type)
    {
        yield return new TestCorrelationProperty("CorrelationId", type);
    }

    class Type1 : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; }
    }

    class Type2 : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public string CorrelationId { get; set; }
    }
}