using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        [TestCase("mark persistent saga data as complete")]
        [TestCase("mark non-persistent saga data as complete")]
        public void EventAreFiredInTheRightPlaces(string scenario)
        {
            var events = new List<string>();
            const string justTheSameFrigginId = "just the same friggin ID";

            var fixture = new SagaFixture<CompletionSagaData>(new CompletionSaga());
            fixture.MarkedAsComplete += (msg, data) => events.Add("markedAsComplete");

            switch (scenario)
            {
                case "mark persistent saga data as complete":
                    fixture.Handle(new InitiatingMessage { CorrelationId = justTheSameFrigginId });
                    fixture.Handle(new CompletionMessage { CorrelationId = justTheSameFrigginId });
                    break;

                case "mark non-persistent saga data as complete":
                    fixture.Handle(new InitiateAndCompleteMessage { CorrelationId = justTheSameFrigginId });
                    break;

                default:
                    throw new ArgumentException(string.Format("Unknown scenario: {0}", scenario));
            }
        
            events.ShouldContain("markedAsComplete");
        }


        [Test]
        public void CanCorrectlyHandleWhenSagaIsMarkedAsComplete()
        {
            // arrange
            var fixture = new SagaFixture<CompletionSagaData>(new CompletionSaga());
            const string justTheSameFrigginId = "just the same friggin ID";

            var sagaDatasMarkedAsComplete = new List<ISagaData>();
            fixture.MarkedAsComplete += (message, data) => sagaDatasMarkedAsComplete.Add(data);
            fixture.CorrelatedWithExistingSagaData += (message, data) => Console.WriteLine("Correlated!");
            fixture.CouldNotCorrelate += message => Console.WriteLine("Could not correlate!");
            fixture.CreatedNewSagaData += (message, data) => Console.WriteLine("Created new!");
            fixture.Exception += (message, exception) => Console.WriteLine("Exception!");

            // act
            fixture.Handle(new InitiatingMessage { CorrelationId = justTheSameFrigginId });
            fixture.Handle(new CompletionMessage { CorrelationId = justTheSameFrigginId });
            fixture.Handle(new InitiatingMessage { CorrelationId = justTheSameFrigginId });
            fixture.Handle(new CompletionMessage { CorrelationId = justTheSameFrigginId });

            // assert
            sagaDatasMarkedAsComplete.Count.ShouldBe(2);
            fixture.DeletedSagaData.Count.ShouldBe(2);
        }

        class InitiatingMessage
        {
            public string CorrelationId { get; set; }
        }

        class CompletionMessage
        {
            public string CorrelationId { get; set; }
        }
        class InitiateAndCompleteMessage
        {
            public string CorrelationId { get; set; }
        }

        class CompletionSaga : Saga<CompletionSagaData>
            , IAmInitiatedBy<InitiatingMessage>
            , IHandleMessages<CompletionMessage>
            , IAmInitiatedBy<InitiateAndCompleteMessage>
        {
            static int instanceCounter = 1;

            public static void ResetInstanceCounter()
            {
                instanceCounter = 0;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<InitiatingMessage>(m => m.CorrelationId).CorrelatesWith(d => d.CorrelationId);
                Incoming<CompletionMessage>(m => m.CorrelationId).CorrelatesWith(d => d.CorrelationId);
                Incoming<InitiateAndCompleteMessage>(m => m.CorrelationId).CorrelatesWith(d => d.CorrelationId);
            }

            public void Handle(InitiatingMessage message)
            {
                Data.CorrelationId = message.CorrelationId;
                Data.InstanceNumber = instanceCounter++;
            }

            public void Handle(CompletionMessage message)
            {
                MarkAsComplete();
            }

            public void Handle(InitiateAndCompleteMessage message)
            {
                MarkAsComplete();
            }
        }

        class CompletionSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string CorrelationId { get; set; }
            public int InstanceNumber { get; set; }
        }

        [Test]
        public void WorksWhenMessageReferenceIsOfTheSupertype()
        {
            // arrange
            var data = new CounterpartData { Dcid = 800 };
            var calledHandlers = new List<string>();
            var fixture = new SagaFixture<CounterpartData>(new CounterpartUpdater(calledHandlers));
            fixture.AddSagaData(data);
            CounterpartChanged messageSupertype1 = new CounterpartCreated { Dcid = 800 };
            CounterpartChanged messageSupertype2 = new CounterpartUpdated { Dcid = 800 };

            // act
            // assert
            fixture.Handle(messageSupertype1);
            fixture.Handle(messageSupertype2);

            calledHandlers.ShouldBe(new List<string>
                {
                    "CounterpartCreated",
                    "CounterpartUpdated",
                });
        }

        [Test]
        public void AddShouldAddToAvailableSagaData()
        {
            var someSagaData = new SomeSagaData { JustSomeText = Guid.NewGuid().ToString()};
            
            var fixture = new SagaFixture<SomeSagaData>(new SomeSaga()) {someSagaData};

            fixture.OfType<SomeSagaData>().First().JustSomeText.ShouldBe(someSagaData.JustSomeText);
        }

        public class CounterpartUpdater : Saga<CounterpartData>,
            IAmInitiatedBy<CounterpartCreated>,
            IAmInitiatedBy<CounterpartUpdated>
        {
            readonly IList<string> calledHandlers;

            public CounterpartUpdater(IList<string> calledHandlers)
            {
                this.calledHandlers = calledHandlers;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<CounterpartCreated>(m => m.Dcid).CorrelatesWith(d => d.Dcid);
                Incoming<CounterpartUpdated>(m => m.Dcid).CorrelatesWith(d => d.Dcid);
            }

            public void Handle(CounterpartCreated message)
            {
                calledHandlers.Add("CounterpartCreated");
            }

            public void Handle(CounterpartUpdated message)
            {
                calledHandlers.Add("CounterpartUpdated");
            }

            public void Handle(CounterpartChanged message)
            {
                calledHandlers.Add("CounterpartChanged");
            }
        }

        public class CounterpartData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public int Dcid { get; set; }
        }

        public abstract class CounterpartChanged
        {
            public int Dcid { get; set; }
        }

        public class CounterpartCreated : CounterpartChanged { }

        public class CounterpartUpdated : CounterpartChanged { }

        [TestCase("in the list", Ignore = true, Description = "don't want to provide this option anymore")]
        [TestCase("after the fact")]
        public void CanUseSagaDataProvidedInVariousWays(string howToProvideSagaData)
        {
            // arrange
            const string recognizableText = "this, I can recognize!";
            var someSagaData = new SomeSagaData { SagaDataId = 10, JustSomeText = recognizableText };
            SagaFixture<SomeSagaData> fixture;

            switch (howToProvideSagaData)
            {
                //case "in the list":
                //    fixture = new SagaFixture<SomeSagaData>(new SomeSaga(), new List<SomeSagaData> { someSagaData });
                //    break;

                case "after the fact":
                    fixture = new SagaFixture<SomeSagaData>(new SomeSaga());
                    fixture.AddSagaData(someSagaData);
                    break;

                default:
                    throw new ArgumentException(string.Format("Don't know how to provide saga data like this: {0}", howToProvideSagaData), "howToProvideSagaData");
            }

            // act
            fixture.Handle(new SomeMessage { SagaDataId = 10 });

            // assert
            var availableSagaData = fixture.AvailableSagaData;
            availableSagaData.Count().ShouldBe(1);
            var sagaDataClone = availableSagaData.Single(d => d.SagaDataId == 10);
            sagaDataClone.JustSomeText.ShouldBe(recognizableText);
        }

        [Test]
        public void CanCorrelateMessagesLikeExpected()
        {
            // arrange
            var fixture = new SagaFixture<SomeSagaData>(new SomeSaga());

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
            var availableSagaData = fixture.AvailableSagaData;
            availableSagaData.Count().ShouldBe(2);
            availableSagaData.Single(d => d.SagaDataId == 10).ReceivedMessages.ShouldBe(2);
            availableSagaData.Single(d => d.SagaDataId == 12).ReceivedMessages.ShouldBe(3);
        }

        [Test]
        public void GivesEasyAccessToTheMostRecentlyCorrelatedSagaData()
        {
            // arrange
            var fixture = new SagaFixture<SomeSagaData>(new SomeSaga());

            fixture.CreatedNewSagaData += (message, data) => Console.WriteLine("Created new saga data");
            fixture.CorrelatedWithExistingSagaData += (message, data) => Console.WriteLine("Correlated with existing saga data");
            fixture.CouldNotCorrelate += message => Console.WriteLine("Could not correlate");

            // act
            fixture.Handle(new SomeMessage { SagaDataId = 10 });
            var data10Created = fixture.Data;

            fixture.Handle(new SomeMessage { SagaDataId = 10 });
            var data10Correlated = fixture.Data;

            fixture.Handle(new SomeMessage { SagaDataId = 12 });
            var data12Created = fixture.Data;

            fixture.Handle(new SomeMessage { SagaDataId = 12 });
            var data12Correlated = fixture.Data;

            // assert
            data10Created.SagaDataId.ShouldBe(10);
            data10Correlated.SagaDataId.ShouldBe(10);
            data12Created.SagaDataId.ShouldBe(12);
            data12Correlated.SagaDataId.ShouldBe(12);
        }

        [Test]
        public void GetsHumanReadableExceptionWhenSomethingGoesWrong()
        {
            // arrange
            var data = new List<SomeSagaData> { new SomeSagaData { SagaDataId = 23 } };
            var fixture = new SagaFixture<SomeSagaData>(new SomeSaga());
            fixture.AddSagaData(data);

            // act
            var exception = Assert.Throws<ApplicationException>(() => fixture.Handle(new SomePoisonMessage { SagaDataId = 23 }));

            Console.WriteLine(exception.ToString());

            // assert
            exception.Message.ShouldContain("Oh no, something bad happened while processing message with saga data id 23");
        }

        [Test]
        public void WorksWithAsyncHandling()
        {
            // arrange
            var fixture = new SagaFixture<SomeSagaData>(new SomeSaga());

            fixture.CreatedNewSagaData += (message, data) => Console.WriteLine("Created new saga data");
            fixture.CorrelatedWithExistingSagaData += (message, data) => Console.WriteLine("Correlated with existing saga data");
            fixture.CouldNotCorrelate += message => Console.WriteLine("Could not correlate");

            // act
            fixture.Handle(new SomeMessage { SagaDataId = 33 });
            fixture.Handle(new SomeMessageForAsync { SagaDataId = 33 });

            // assert
            var availableSagaData = fixture.AvailableSagaData.Single();
            availableSagaData.SagaDataId.ShouldBe(33);
            availableSagaData.ReceivedMessages.ShouldBe(2);
        }

        [Test]
        public void WorksWithAsyncInitiator()
        {
            // arrange
            var fixture = new SagaFixture<SomeSagaData>(new SomeSaga());

            fixture.CreatedNewSagaData += (message, data) => Console.WriteLine("Created new saga data");
            fixture.CorrelatedWithExistingSagaData += (message, data) => Console.WriteLine("Correlated with existing saga data");
            fixture.CouldNotCorrelate += message => Console.WriteLine("Could not correlate");

            // act
            fixture.Handle(new SomeAsyncMessage { SagaDataId = 33 });
            fixture.Handle(new SomeMessage { SagaDataId = 33 });

            // assert
            var availableSagaData = fixture.AvailableSagaData.Single();
            availableSagaData.SagaDataId.ShouldBe(33);
            availableSagaData.ReceivedMessages.ShouldBe(2);
        }

        class SomeMessage
        {
            public int SagaDataId { get; set; }
        }

        class SomeAsyncMessage
        {
            public int SagaDataId { get; set; }
        }

        class SomePoisonMessage
        {
            public int SagaDataId { get; set; }
        }

        class SomeMessageForAsync
        {
            public int SagaDataId { get; set; }
        }

        class SomeSaga : Saga<SomeSagaData>,
            IAmInitiatedBy<SomeMessage>,
            IAmInitiatedByAsync<SomeAsyncMessage>,
            IHandleMessages<SomePoisonMessage>,
            IHandleMessagesAsync<SomeMessageForAsync>
        {
            public override void ConfigureHowToFindSaga()
            {
                Incoming<SomeMessage>(m => m.SagaDataId).CorrelatesWith(d => d.SagaDataId);
                Incoming<SomeAsyncMessage>(m => m.SagaDataId).CorrelatesWith(d => d.SagaDataId);
                Incoming<SomePoisonMessage>(m => m.SagaDataId).CorrelatesWith(d => d.SagaDataId);
                Incoming<SomeMessageForAsync>(m => m.SagaDataId).CorrelatesWith(d => d.SagaDataId);
            }

            public void Handle(SomeMessage message)
            {
                if (IsNew)
                {
                    Data.SagaDataId = message.SagaDataId;
                }

                Data.ReceivedMessages++;
            }

            public async Task Handle(SomeAsyncMessage message)
            {
                await Task.Yield();

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

            public async Task Handle(SomeMessageForAsync message)
            {
                await Task.Yield();

                Data.ReceivedMessages++;
            }
        }

        class SomeSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }

            public int SagaDataId { get; set; }

            public int ReceivedMessages { get; set; }
            public string JustSomeText { get; set; }
        }
    }
}