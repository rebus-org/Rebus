using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Shouldly;
using System.Linq;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestAutoCorrelation : RebusBusMsmqIntegrationTestBase
    {
        const string SagaBusInputQueueName = "test.autocorrelation.saga";
        const string ServiceBusInputQueueName = "test.autocorrelation.service";
        RebusBus sagaBus;
        RebusBus serviceBus;
        RebusBus initiatorBus;
        HandlerActivatorForTesting serviceHandlerActivator;
        HandlerActivatorForTesting sagaHandlerActivator;
        InMemorySagaPersister sagaPersister;

        protected override void DoSetUp()
        {
            // this is the bus hosting the saga
            sagaHandlerActivator = new HandlerActivatorForTesting();
            sagaPersister = new InMemorySagaPersister();
            sagaBus = CreateBus(SagaBusInputQueueName, sagaHandlerActivator, new InMemorySubscriptionStorage(), sagaPersister, "error").Start(1);

            // this is the "service bus", i.e. just some request/reply-enabled service somewhere
            serviceHandlerActivator = new HandlerActivatorForTesting();
            serviceBus = CreateBus(ServiceBusInputQueueName, serviceHandlerActivator).Start(1);

            // this is just a bus from the outside that can initiate everything
            initiatorBus = CreateBus("test.autocorrelation.initiator", new HandlerActivatorForTesting()).Start(1);
        }

        [Test]
        public void ItWorks()
        {
            // arrange
            var someSaga = new SomeSaga(sagaBus);
            sagaHandlerActivator.UseHandler(someSaga);
            
            serviceHandlerActivator.Handle<Request>(r =>
                {
                    TestHelpers.DumpHeadersFromCurrentMessageContext();
                    serviceBus.Reply(new Reply());
                });

            // act
            initiatorBus.Send(new InitiateStuff());
            Thread.Sleep(1.Seconds());

            // assert
            sagaPersister.Count().ShouldBe(1);
            var sagaData = sagaPersister.Single();
            sagaData.ShouldBeTypeOf<SomeSagaData>();
            ((SomeSagaData) sagaData).GotTheReply.ShouldBe(true);
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(Request))
            {
                return ServiceBusInputQueueName;
            }

            if (messageType == typeof(InitiateStuff))
            {
                return SagaBusInputQueueName;
            }

            return base.GetEndpointFor(messageType);
        }
    }

    class InitiateStuff{}
    class Request{}
    class Reply{}

    class SomeSaga : Saga<SomeSagaData>,
        IAmInitiatedBy<InitiateStuff>,
        IHandleMessages<Reply>
    {
        readonly IBus bus;

        public SomeSaga(IBus bus)
        {
            this.bus = bus;
        }

        public override void ConfigureHowToFindSaga()
        {
            
        }

        public void Handle(InitiateStuff message)
        {
            bus.Send(new Request());
        }

        public void Handle(Reply message)
        {
            TestHelpers.DumpHeadersFromCurrentMessageContext();

            Data.GotTheReply = true;
        }
    }

    class SomeSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }

        public bool GotTheReply { get; set; }
    }
}