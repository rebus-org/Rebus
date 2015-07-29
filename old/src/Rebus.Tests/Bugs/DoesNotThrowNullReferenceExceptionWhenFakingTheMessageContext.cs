using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Testing;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Description("Looked like a bug but turned out to be an exception that was hard to diagnose")]
    public class DoesNotThrowNullReferenceExceptionWhenFakingTheMessageContext
    {
        [SetUp]
        public void SetUp()
        {
            FakeMessageContext.Reset();
        }

        [Test]
        public void ThrowsProperExceptionWhenAttemptingToEstablishMessageContextWithItemsDictionary()
        {
            var saga = new SearchSaga();
            var fixture = new SagaFixture<SearchSagaData>(saga);
            var fakeContext = MockRepository.GenerateMock<IMessageContext>();
            fakeContext.Stub(s => s.ReturnAddress).Return("queuename");
            fakeContext.Stub(s => s.Headers).Return(new Dictionary<string, object>());

            // act
            var ex = Assert
                .Throws<ArgumentException>(() =>
                    {
                        using (TransactionContext.None())
                        using (FakeMessageContext.Establish(fakeContext))
                        {
                            fixture.Handle("w00000t!");
                        }
                    });

            ex.Message.ShouldContain("has null as the Items property");
        }
        public class SearchSaga : Saga<SearchSagaData>
        {
            public override void ConfigureHowToFindSaga()
            {
            }
        }

        public class SearchSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }
    }
}