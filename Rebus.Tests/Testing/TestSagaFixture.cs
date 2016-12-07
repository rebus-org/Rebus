using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Sagas;
using Rebus.Testing;
using Rebus.Tests.Contracts;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Testing
{
    public class TestSagaFixture : FixtureBase
    {
        [Fact]
        public void CanSetUpFakeSagaData()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                fixture.Add(new MySagaState {Text = "I know you!"});
                fixture.AddRange(new[] { new MySagaState { Text = "I know you too!" } });

                Assert.Equal(2, fixture.Data.Count());
                Assert.Equal(1, fixture.Data.OfType<MySagaState>().Count(d => d.Text == "I know you!"));
                Assert.Equal(1, fixture.Data.OfType<MySagaState>().Count(d => d.Text == "I know you too!"));
            }
        }

        [Fact]
        public void CanRetrieveSagaData()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                fixture.Deliver(new TestMessage("hej"));

                var current = fixture.Data.OfType<MySagaState>().ToList();

                Assert.Equal(1, current.Count);
                Assert.Equal("hej", current[0].Text);
            }
        }

        [Fact]
        public void EmitsCouldNotCorrelateEvent()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                var gotEvent = false;
                fixture.CouldNotCorrelate += () => gotEvent = true;

                fixture.Deliver(new TestMessage("hej"));

                Assert.True(gotEvent);
            }
        }

        [Fact]
        public void EmitsCreatedEvent()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                var gotEvent = false;
                fixture.Created += d => gotEvent = true;

                fixture.Deliver(new TestMessage("hej"));

                Assert.True(gotEvent);
            }
        }

        [Fact]
        public void EmitsUpdatedEvent()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                fixture.Deliver(new TestMessage("hej"));

                var gotEvent = false;
                fixture.Updated += d => gotEvent = true;

                fixture.Deliver(new TestMessage("hej"));

                Assert.True(gotEvent);
            }
        }

        [Fact]
        public void EmitsDeletedEvent()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                fixture.Deliver(new TestMessage("hej"));

                var gotEvent = false;
                fixture.Deleted += d => gotEvent = true;

                fixture.Deliver(new TestMessage("hej") { Die = true });

                Assert.True(gotEvent);
            }
        }

        [Fact]
        public void DoesNotTimeOutWhenDebuggerIsAttached()
        {
            
        }

        ///<summary>
        /// SagaData with non-empty Id is added to SagaFixture.Data.
        ///</summary>
        [Fact]
        public void SagaDataWithNonEmptyIdIsAddedToSagaFixtureData()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                // Arrange

                // Act
                fixture.Add(new MySagaState { Id = Guid.NewGuid() });

                // Assert
                Assert.Equal(1, fixture.Data.Count());
            }
        }

        ///<summary>
        /// Verify that Id is set upon null Id.
        ///</summary>
        [Fact]
        public void IdIsSetUponNullId()
        {
            using (var fixture = SagaFixture.For<MySaga>())
            {
                // Arrange

                // Act
                fixture.Add(new MySagaState());

                // Asert
                Assert.Equal(1, fixture.Data.Count());
                Assert.NotNull(fixture.Data.Single().Id);
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