using System;
using NUnit.Framework;
using Ponder;
using Rebus.Tests.Persistence.Sagas.Factories;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Persistence.Sagas
{
    [TestFixture(typeof(RavenDbSagaPersisterFactory), Category = TestCategories.Raven)]
    public class TestSagaPersistersEmployingTransactions<TFactory> : TestSagaPersistersBase<TFactory> where TFactory : ISagaPersisterFactory
    {
        [Test]
        public void TwoSagasWithSameCorrelationWillBothBeFound()
        {
            const string theValue = "this just happens to be the same in two sagas";
            var firstSaga = new SomeSaga { Id = Guid.NewGuid(), SomeCorrelationId = theValue };
            var secondSaga = new SomeSaga { Id = Guid.NewGuid(), SomeCorrelationId = theValue };

            var pathsToIndex = new[] { Reflect.Path<SomeSaga>(s => s.SomeCorrelationId) };
            Persister.Insert(firstSaga, pathsToIndex);
            Persister.Insert(secondSaga, pathsToIndex);

            var sagas = Persister.Find<SomeSaga>(Reflect.Path<SomeSaga>(s => s.SomeCorrelationId), theValue).ToList();
            
            sagas.Count().ShouldBe(2);
            sagas.ShouldContain(x => x.Id == firstSaga.Id);
            sagas.ShouldContain(x => x.Id == secondSaga.Id);
        }

        internal class SomeSaga : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string SomeCorrelationId { get; set; }
        }
    }
}