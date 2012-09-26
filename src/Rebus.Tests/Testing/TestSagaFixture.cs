using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rebus.Testing;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Testing
{
    /// <summary>
    /// Yo dawg, I heard you like testing, so I wrote a test that tested your test...
    /// </summary>
    [TestFixture]
    public class TestSagaFixture : FixtureBase
    {
        [Test]
        public void CanCorrelateMessagesLikeExpected()
        {
            // arrange
            var availableSagaData = new List<SomeSagaData>();
            var fixture = new SagaFixture<SomeSagaData>(new SomeSaga(), availableSagaData);

            fixture.CreatedNewSagaData += (message, data) => Console.WriteLine("Created new saga data");
            fixture.CorrelatedWithExistingSagaData += (message, data) => Console.WriteLine("Correlated with existing saga data");
            fixture.CouldNotCorrelate += message => Console.WriteLine("Could not correlate");

            // act
            fixture.Handle(new SomeMessage { SagaDataId = 10 });
            fixture.Handle(new SomeMessage { SagaDataId = 10 });
            fixture.Handle(new SomeMessage { SagaDataId = 12 });
            fixture.Handle(new SomeMessage { SagaDataId = 12 });
            fixture.Handle(new SomeMessage { SagaDataId = 12 });

            // assert
            availableSagaData.Count.ShouldBe(2);
            availableSagaData.Single(d => d.SagaDataId == 10).ReceivedMessages.ShouldBe(2);
            availableSagaData.Single(d => d.SagaDataId == 12).ReceivedMessages.ShouldBe(3);
        }

        [Test]
        public void GetsHumanReadableExceptionWhenSomethingGoesWrong()
        {
            // arrange
            var data = new List<SomeSagaData> {new SomeSagaData {SagaDataId = 23}};
            var fixture = new SagaFixture<SomeSagaData>(new SomeSaga(), data);

            // act
            var exception = Assert.Throws<ApplicationException>(() => fixture.Handle(new SomePoisonMessage {SagaDataId = 23}));

            Console.WriteLine(exception.ToString());

            // assert
            exception.Message.ShouldContain("Oh no, something bad happened while processing message with saga data id 23");
        }

        class SomeMessage
        {
            public int SagaDataId { get; set; }
        }

        class SomePoisonMessage
        {
            public int SagaDataId { get; set; }
        }

        class SomeSaga : Saga<SomeSagaData>,
            IAmInitiatedBy<SomeMessage>,
            IHandleMessages<SomePoisonMessage>
        {
            public override void ConfigureHowToFindSaga()
            {
                Incoming<SomeMessage>(m => m.SagaDataId).CorrelatesWith(d => d.SagaDataId);
                Incoming<SomePoisonMessage>(m => m.SagaDataId).CorrelatesWith(d => d.SagaDataId);
            }

            public void Handle(SomeMessage message)
            {
                if (IsNew)
                {
                    Data.SagaDataId = message.SagaDataId;
                }

                Data.ReceivedMessages++;
            }

            public void Handle(SomePoisonMessage message)
            {
                throw new ApplicationException(string.Format("Oh no, something bad happened while processing message with saga data id {0}", message.SagaDataId));
            }
        }

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public int SagaDataId { get; set; }

            public int ReceivedMessages { get; set; }
        }
    }
}