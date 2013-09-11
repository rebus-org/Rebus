using System;
using System.Collections.Generic;
using Rebus.Shared;
using Rebus.Testing;
using Rhino.Mocks;

namespace Rebus.Tests.Persistence.Sagas
{
    public class TestSagaPersistersBase<TFactory> : FixtureBase where TFactory : ISagaPersisterFactory
    {
        MessageContext messageContext;
        TFactory factory;
        protected IStoreSagaData persister;

        protected override void DoSetUp()
        {
            factory = Activator.CreateInstance<TFactory>();
            var headers = new Dictionary<string, object>
                {
                    {Headers.ReturnAddress, "none"},
                    {Headers.MessageId, "just_some_message_id"},
                };
            messageContext = MessageContext.Establish(headers);
            persister = factory.CreatePersister();
        }

        protected override void DoTearDown()
        {
            factory.Dispose();
            messageContext.Dispose();
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