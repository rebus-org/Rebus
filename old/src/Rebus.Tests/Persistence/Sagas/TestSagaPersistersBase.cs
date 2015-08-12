using System;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Shared;
using Rebus.Testing;
using Rhino.Mocks;

namespace Rebus.Tests.Persistence.Sagas
{
    public class TestSagaPersistersBase<TFactory> : FixtureBase where TFactory : ISagaPersisterFactory
    {
        MessageContext messageContext;
        protected IStoreSagaData persister;

        protected override void DoSetUp()
        {
            var headers = new Dictionary<string, object>
                {
                    {Headers.ReturnAddress, "none"},
                    {Headers.MessageId, "just_some_message_id"},
                };
            persister = TrackDisposable(Activator.CreateInstance<TFactory>()).CreatePersister();

            TrackDisposable(TransactionContext.None());
            messageContext = MessageContext.Establish(headers);
        }

        protected override void DoTearDown()
        {
            if (messageContext != null)
            {
                messageContext.Dispose();
            }
        }

        protected void ReturnToOriginalMessageContext()
        {
            FakeMessageContext.Establish(messageContext);
        }

        protected void EnterAFakeMessageContext()
        {
            var fakeConcurrentMessageContext = Mock<IMessageContext>();
            fakeConcurrentMessageContext.Stub(x => x.Headers).Return(new Dictionary<string, object>());
            fakeConcurrentMessageContext.Stub(x => x.Items).Return(new Dictionary<string, object>());
            FakeMessageContext.Establish(fakeConcurrentMessageContext);
        }

        protected class GenericSagaData<T> : ISagaData
        {
            public T Property { get; set; }
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        protected class MySagaData : ISagaData
        {
            public string SomeField { get; set; }
            public string AnotherField { get; set; }
            public SomeEmbeddedThingie Embedded { get; set; }
            public Guid Id { get; set; }

            public int Revision { get; set; }
        }

        protected class SimpleSagaData : ISagaData
        {
            public string SomeString { get; set; }
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        protected class SomeCollectedThing
        {
            public int No { get; set; }
        }

        protected class SomeEmbeddedThingie
        {
            public SomeEmbeddedThingie()
            {
                Thingies = new List<SomeCollectedThing>();
            }

            public string ThisIsEmbedded { get; set; }
            public List<SomeCollectedThing> Thingies { get; set; }
        }
    }
}