using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Testing;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Testing
{
    [TestFixture]
    public class TestSagaFixtureAndSerializationCornerCases : FixtureBase
    {
        [Test]
        public void CanCorrectlySerializeAndDeserializeCaspersSagaData()
        {
            // arrange
            var fixture = new SagaFixture<SagaData>(new Saga());

            // act
            fixture.Handle(new Message { CorrelationId = 42, String = "hello there" });
            fixture.Handle(new Message { CorrelationId = 42, String = "hello again!" });

            // assert
            fixture.AvailableSagaData.Count()
                   .ShouldBe(1);

            var sagaData = fixture.AvailableSagaData.Single();
            sagaData.ReceivedStrings.ShouldContain("hello there");
            sagaData.ReceivedStrings.ShouldContain("hello again!");

            var concreteClass = (ConcreteClass) sagaData.RefToAbstractClass;
            concreteClass.WithWhat.ShouldBe("something in it");
        }

        class Saga : Saga<SagaData>, IAmInitiatedBy<Message>
        {
            public override void ConfigureHowToFindSaga()
            {
                Incoming<Message>(s => s.CorrelationId).CorrelatesWith(d => d.CorrelationId);
            }

            public void Handle(Message message)
            {
                if (IsNew)
                {
                    Data.RefToAbstractClass = new ConcreteClass {WithWhat = "something in it"};
                    Data.CorrelationId = message.CorrelationId;
                }

                Data.ReceivedStrings.Add(message.String);
            }
        }

        class Message
        {
            public int CorrelationId { get; set; }
            public string String { get; set; }
        }

        class SagaData : ISagaData
        {
            public SagaData()
            {
                ReceivedStrings = new List<string>();
            }
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public int CorrelationId { get; set; }
            public AbstractClass RefToAbstractClass { get; set; }
            public List<string> ReceivedStrings { get; set; }
        }

        abstract class AbstractClass
        {
        }

        class ConcreteClass : AbstractClass
        {
            public string WithWhat { get; set; }
        }

    }
}