using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Testing;
using Shouldly;
using Rhino.Mocks;

namespace Rebus.Tests.Bugs
{
    [TestFixture]
    public class TestingWorksWithMockedMessageContextAlsoWhenAborting : FixtureBase
    {
        SagaFixture<ChunkOfData> fixture;
        TheSaga sagaInstance;

        protected override void DoSetUp()
        {
            sagaInstance = new TheSaga();
            fixture = new SagaFixture<ChunkOfData>(sagaInstance);
        }

        [Test]
        public void SureDoes()
        {
            // arrange
            var mock = Mock<IMessageContext>();
            mock.Stub(m => m.Items).Return(new Dictionary<string, object>());
            mock.Stub(m => m.Headers).Return(new Dictionary<string, object>());
            mock.Stub(m => m.HandlersToSkip).Return(new List<Type>());

            // act
            using(TransactionContext.None())
            using (FakeMessageContext.Establish(mock))
            {
                fixture.Handle("hello there!");
            }

            // assert
            sagaInstance.GotMessage.ShouldBe(true);
        }
    }

    public class TheSaga : Saga<ChunkOfData>, IAmInitiatedBy<string>
    {
        public bool GotMessage { get; set; }

        public override void ConfigureHowToFindSaga()
        {
        }

        public void Handle(string message)
        {
            Console.WriteLine("Got message: {0}", message);
            GotMessage = true;
        }
    }

    public class ChunkOfData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
    }
}