using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using Rebus.Shared;
using Rebus.Timeout;
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
        TimeoutService timeoutService;

        protected override void DoSetUp()
        {
            MsmqUtil.Delete(SagaBusInputQueueName);
            MsmqUtil.Delete(ServiceBusInputQueueName);

            // this is the bus hosting the saga
            sagaHandlerActivator = new HandlerActivatorForTesting();
            sagaPersister = new InMemorySagaPersister();
            sagaBus = CreateBus(SagaBusInputQueueName, sagaHandlerActivator, new InMemorySubscriptionStorage(), sagaPersister, "error").Start(1);

            // this is the "service bus", i.e. just some request/reply-enabled service somewhere
            serviceHandlerActivator = new HandlerActivatorForTesting();
            serviceBus = CreateBus(ServiceBusInputQueueName, serviceHandlerActivator).Start(1);

            // this is just a bus from the outside that can initiate everything
            initiatorBus = CreateBus("test.autocorrelation.initiator", new HandlerActivatorForTesting()).Start(1);

            timeoutService = new TimeoutService(new InMemoryTimeoutStorage());
            timeoutService.Start();
        }

        protected override void DoTearDown()
        {
            timeoutService.Stop();

            MsmqUtil.Delete(SagaBusInputQueueName);
            MsmqUtil.Delete(ServiceBusInputQueueName);
        }

        [TestCase(true, Description = "Verifies that automatic correlation is NOT performed when a correlation is explicitly set up. This is because we want to preserve the ability to 'abandon' outstanding replies by changing a correlation ID on the saga")]
        [TestCase(false, Description = "Verifies that automatic correlation is NOT performed when a correlation is explicitly set up. This is because we want to preserve the ability to 'abandon' outstanding replies by changing a correlation ID on the saga")]
        public void IgnoresAutoCorrelationWhenCorrelationIsExplicitlySetUp(bool abandonReply)
        {
            // arrange
            var anotherSaga = new AnotherSaga(sagaBus, abandonReply);
            sagaHandlerActivator.UseHandler(anotherSaga);

            serviceHandlerActivator.Handle<RequestWithCorrelationId>(req => serviceBus.Reply(new ReplyWithCorrelationId { Correlationid = req.Correlationid }));

            // act
            initiatorBus.Send(new InitiateRequestReply());
            Thread.Sleep(3.Seconds());

            // assert
            sagaPersister.Count().ShouldBe(1);
            var sagaData = sagaPersister.Single();
            sagaData.ShouldBeOfType<SomeSagaData>();
            ((SomeSagaData)sagaData).GotTheReply.ShouldBe(!abandonReply);
        }

        [Test]
        public void WorksWithDeferredMessageAsWell()
        {
            // arrange
            var someSaga = new SomeSaga(sagaBus);
            sagaHandlerActivator.UseHandler(someSaga);

            // act
            initiatorBus.Send(new InitiateDeferredMessage());
            Thread.Sleep(3.Seconds());

            // assert
            sagaPersister.Count().ShouldBe(1);
            var sagaData = sagaPersister.Single();
            sagaData.ShouldBeOfType<SomeSagaData>();
            ((SomeSagaData)sagaData).GotTheDeferredMessage.ShouldBe(true);
        }

        [Test]
        public void WorksWithOrdinaryRequestReply()
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
            initiatorBus.Send(new InitiateRequestReply());
            Thread.Sleep(1.Seconds());

            // assert
            sagaPersister.Count().ShouldBe(1);
            var sagaData = sagaPersister.Single();
            sagaData.ShouldBeOfType<SomeSagaData>();
            ((SomeSagaData)sagaData).GotTheReply.ShouldBe(true);
        }

        public override string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(Request))
            {
                return ServiceBusInputQueueName;
            }

            if (messageType == typeof(RequestWithCorrelationId))
            {
                return ServiceBusInputQueueName;
            }

            if (messageType == typeof(InitiateRequestReply))
            {
                return SagaBusInputQueueName;
            }

            if (messageType == typeof(InitiateDeferredMessage))
            {
                return SagaBusInputQueueName;
            }

            return base.GetEndpointFor(messageType);
        }
    }

    class InitiateRequestReply { }
    class InitiateDeferredMessage { }
    class DeferredMessage { }
    class Request { }
    class Reply { }
    
    class RequestWithCorrelationId
    {
        public string Correlationid { get; set; }
    }

    class ReplyWithCorrelationId
    {
        public string Correlationid { get; set; }
    }

    class AnotherSaga : Saga<SomeSagaData>,
        IAmInitiatedBy<InitiateRequestReply>,
        IHandleMessages<ReplyWithCorrelationId>
    {
        readonly IBus bus;
        readonly bool abandonReply;

        public AnotherSaga(IBus bus, bool abandonReply)
        {
            this.bus = bus;
            this.abandonReply = abandonReply;
        }

        public override void ConfigureHowToFindSaga()
        {
            Incoming<ReplyWithCorrelationId>(m => m.Correlationid).CorrelatesWith(d => d.CorrelationId);
        }

        public void Handle(InitiateRequestReply message)
        {
            Data.CorrelationId = "<this is a correlation ID!>";

            bus.Send(new RequestWithCorrelationId {Correlationid = Data.CorrelationId});

            if (abandonReply)
            {
                Data.CorrelationId = "<this is another correlation ID, which is how we may abandon outstanding replies>";
            }
        }

        public void Handle(ReplyWithCorrelationId message)
        {
            Data.GotTheReply = true;
        }
    }

    class SomeSaga : Saga<SomeSagaData>,
        IAmInitiatedBy<InitiateRequestReply>,
        IHandleMessages<Reply>,
        IAmInitiatedBy<InitiateDeferredMessage>,
        IHandleMessages<DeferredMessage>
    {
        readonly IBus bus;

        public SomeSaga(IBus bus)
        {
            this.bus = bus;
        }

        public override void ConfigureHowToFindSaga()
        {
        }

        public void Handle(InitiateRequestReply message)
        {
            bus.Send(new Request());
        }

        public void Handle(Reply message)
        {
            TestHelpers.DumpHeadersFromCurrentMessageContext();

            Data.GotTheReply = true;
        }

        public void Handle(InitiateDeferredMessage message)
        {
            bus.Defer(1.Seconds(), new DeferredMessage());
        }

        public void Handle(DeferredMessage message)
        {
            Data.GotTheDeferredMessage = true;
        }
    }

    class SomeSagaData : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }

        public bool GotTheReply { get; set; }
        public bool GotTheDeferredMessage { get; set; }

        public string CorrelationId { get; set; }
    }
}