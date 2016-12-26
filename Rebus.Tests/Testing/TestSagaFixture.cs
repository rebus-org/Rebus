using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Sagas;
using Rebus.Testing;
using Rebus.Tests.Contracts;
using Rebus.Tests.Sagas;

#pragma warning disable 1998

namespace Rebus.Tests.Testing
{
    [TestFixture]
    public class TestSagaFixture : FixtureBase
    {
        [Test]
        public void CanSetUpFakeSagaData()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                fixture.Add(new MySagaState {Text = "I know you!"});
                fixture.AddRange(new[] { new MySagaState { Text = "I know you too!" } });

                Assert.That(fixture.Data.Count(), Is.EqualTo(2));
                Assert.That(fixture.Data.OfType<MySagaState>().Count(d => d.Text == "I know you!"), Is.EqualTo(1));
                Assert.That(fixture.Data.OfType<MySagaState>().Count(d => d.Text == "I know you too!"), Is.EqualTo(1));
            }
        }

        [Test]
        public void CanRetrieveSagaData()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                fixture.Deliver(new TestMessage("hej"));

                var current = fixture.Data.OfType<MySagaState>().ToList();

                Assert.That(current.Count, Is.EqualTo(1));
                Assert.That(current[0].Text, Is.EqualTo("hej"));
            }
        }

        [Test]
        public void EmitsCouldNotCorrelateEvent()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                var gotEvent = false;
                fixture.CouldNotCorrelate += () => gotEvent = true;

                fixture.Deliver(new TestMessage("hej"));

                Assert.That(gotEvent, Is.True);
            }
        }

        [Test]
        public void EmitsCreatedEvent()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                var gotEvent = false;
                fixture.Created += d => gotEvent = true;

                fixture.Deliver(new TestMessage("hej"));

                Assert.That(gotEvent, Is.True);
            }
        }

        [Test]
        public void EmitsUpdatedEvent()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                fixture.Deliver(new TestMessage("hej"));

                var gotEvent = false;
                fixture.Updated += d => gotEvent = true;

                fixture.Deliver(new TestMessage("hej"));

                Assert.That(gotEvent, Is.True);
            }
        }

        [Test]
        public void EmitsDeletedEvent()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                fixture.Deliver(new TestMessage("hej"));

                var gotEvent = false;
                fixture.Deleted += d => gotEvent = true;

                fixture.Deliver(new TestMessage("hej") { Die = true });

                Assert.That(gotEvent, Is.True);
            }
        }

        [Test]
        public void DoesNotTimeOutWhenDebuggerIsAttached()
        {
            
        }

        ///<summary>
        /// SagaData with non-empty Id is added to SagaFixture.Data.
        ///</summary>
        [Test]
        public void SagaDataWithNonEmptyIdIsAddedToSagaFixtureData()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                // Arrange

                // Act
                fixture.Add(new MySagaState { Id = Guid.NewGuid() });

                // Assert
                Assert.That(fixture.Data.Count(), Is.EqualTo(1));
            }
        }

        ///<summary>
        /// Verify that Id is set upon null Id.
        ///</summary>
        [Test]
        public void IdIsSetUponNullId()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                // Arrange

                // Act
                fixture.Add(new MySagaState());

                // Asert
                Assert.That(fixture.Data.Count(), Is.EqualTo(1));
                Assert.That(fixture.Data.Single().Id, Is.Not.Null);
            }
        }

        class MySaga : Saga<MySagaState>, IAmInitiatedBy<TestMessage>
        {
            protected override void CorrelateMessages(ICorrelationConfig<MySagaState> config)
            {
                config.Correlate<TestMessage>(m => m.Text, d => d.Text);
            }

            public async Task Handle(TestMessage message)
            {
                Data.Text = message.Text;

                if (message.Die) MarkAsComplete();
            }
        }

        class TestMessage
        {
            public TestMessage(string text)
            {
                Text = text;
            }

            public string Text { get; }
            public bool Die { get; set; }
        }

        class MySagaState : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
            public string Text { get; set; }
        }
    }
}