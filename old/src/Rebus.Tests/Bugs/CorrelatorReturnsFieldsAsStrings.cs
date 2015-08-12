using System;
using NUnit.Framework;

namespace Rebus.Tests.Bugs
{
    public class CorrelatorReturnsFieldsAsStrings
    {
        [Test]
        public void AndItShouldNotDoThat()
        {
            var newGuid = Guid.NewGuid();
            var correlator = new Correlator<TheSagaData, TheMessage>(
                x => x.Id,
                new TheSaga
                {
                    Data = new TheSagaData
                    {
                        Id = newGuid
                    }
                });

            var fieldFromMessage = correlator.FieldFromMessage(new TheMessage { Id = newGuid });
            Assert.AreEqual(newGuid, fieldFromMessage);
        }

        public class TheMessage
        {
            public Guid Id { get; set; }
        }

        public class TheSaga : Saga<TheSagaData>
        {
            public override void ConfigureHowToFindSaga()
            {
                Incoming<TheMessage>(x => x.Id).CorrelatesWith(x => x.Id);
            }
        }

        public class TheSagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }
    }
}