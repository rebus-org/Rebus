using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Serialization.Json;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestSagas : FixtureBase
    {
        RebusBus bus;
        HandlerActivatorForTesting handlerActivator;
        MessageReceiverForTesting receiver;
        SagaDataPersisterForTesting storeSagaData;

        protected override void DoSetUp()
        {
            var serializer = new JsonMessageSerializer();
            handlerActivator = new HandlerActivatorForTesting();
            receiver = new MessageReceiverForTesting(serializer);
            storeSagaData = new SagaDataPersisterForTesting();
            bus = new RebusBus(handlerActivator,
                               Mock<ISendMessages>(),
                               receiver,
                               Mock<IStoreSubscriptions>(),
                               Mock<IDetermineDestination>(),
                               serializer,
                               storeSagaData);
            bus.Start();
        }

        [Test]
        public void SagaCanBeInitiatedBySomeMessage()
        {
            var saga = new MySaga();
            handlerActivator.UseHandler(saga);

            Assert.IsNull(saga.Data);

            Deliver(new ShouldInitiateSaga{SomeRandomIncomingData="hello there!"});

            Thread.Sleep(2000);

            Assert.IsTrue(saga.MessageWasInFactDelivered);

            Assert.IsNotNull(saga.Data);
            Assert.AreEqual("hello there!", saga.Data.ThisIsWhatIGot);
        }

        [Test]
        public void SagaIsNotInitiatedAgainWhenIncomingMessageCanBeCorrelatedWithExistingSagaData()
        {
            var saga = new MySaga();
            handlerActivator.UseHandler(saga);

            Assert.IsNull(saga.Data);

            Deliver(new ShouldInitiateSaga {SomeRandomIncomingData = "bla bla bla"});
            Deliver(new ShouldInitiateSaga {SomeRandomIncomingData = "bla bla bla"});

            Thread.Sleep(200);

            Assert.IsNotNull(saga.Data);
            Assert.AreEqual(2, saga.Data.SagaDataChangeCount);
        }

        [Test]
        public void CanHandleMultipleCorrelations()
        {
            var orderIds = new Queue<Guid>();
            var firstOrderId = Guid.NewGuid();
            var secondOrderId = Guid.NewGuid();
            orderIds.Enqueue(firstOrderId);
            orderIds.Enqueue(secondOrderId);
            var saga = new MyComplicatedSaga(orderIds);
            handlerActivator.UseHandler(saga);

            var firstOrderPlaced = new OrderPlaced
                                  {
                                      OrderPlacementId = Guid.NewGuid(),
                                      ProductName = "Beer"
                                  };

            var secondOrderPlaced = new OrderPlaced
                                        {
                                            OrderPlacementId = Guid.NewGuid(),
                                            ProductName = "Another beer"
                                        };
            
            Deliver(firstOrderPlaced);

            Thread.Sleep(500);

            Assert.AreEqual(1, storeSagaData.Count());
            var data = storeSagaData.OfType<ComplicatedSagaData>().Single(d => d.ProductName == "Beer");
            Assert.AreEqual(firstOrderPlaced.OrderPlacementId, data.OrderPlacementId);
        }

        void Deliver(object message)
        {
            receiver.Deliver(new Message {Messages = new[] {message}});
        }

        class ShouldInitiateSaga
        {
            public string SomeRandomIncomingData { get; set; }
        }
        
        class MySagaData : ISagaData
        {
            public Guid Id { get; set; }

            public string ThisIsWhatIGot { get; set; }
            public int SagaDataChangeCount { get; set; }
        }

        class MySaga : Saga<MySagaData>, IAmInitiatedBy<ShouldInitiateSaga>
        {
            public bool MessageWasInFactDelivered { get; set; }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<ShouldInitiateSaga>(m => m.SomeRandomIncomingData).CorrelatesWith(s => s.ThisIsWhatIGot);
            }

            public void Handle(ShouldInitiateSaga message)
            {
                MessageWasInFactDelivered = true;
                Data.ThisIsWhatIGot = message.SomeRandomIncomingData;
                Data.SagaDataChangeCount++;
            }
        }

        enum OrderState
        {
            Init,
            Placed,
            Billed,
            PaymentReceived,
        }

        class ComplicatedSagaData :ISagaData
        {
            public Guid Id { get; set; }

            public Guid OrderPlacementId { get; set; }
            public string ProductName { get; set; }
            public Guid OrderId { get; set; }

            public OrderState OrderState { get; set; }
        }

        class MyComplicatedSaga : Saga<ComplicatedSagaData>, 
            IAmInitiatedBy<OrderPlaced>,
            IHandleMessages<OrderBilled>,
            IHandleMessages<PaymentReceived>
        {
            readonly Queue<Guid> orderIds;

            public MyComplicatedSaga(Queue<Guid> orderIds)
            {
                this.orderIds = orderIds;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<OrderPlaced>(m => m.OrderPlacementId).CorrelatesWith(d => d.OrderPlacementId);
                Incoming<OrderBilled>(m => m.OrderId).CorrelatesWith(d => d.OrderId);
                Incoming<PaymentReceived>(m => m.OrderId).CorrelatesWith(d => d.OrderId);
            }

            public void Handle(OrderPlaced message)
            {
                Assert.AreNotEqual(OrderState.Billed, Data.OrderState);
                Assert.AreNotEqual(OrderState.PaymentReceived, Data.OrderState);

                if (Data.OrderPlacementId == Guid.Empty)
                {
                    Data.OrderPlacementId = message.OrderPlacementId;
                    Data.OrderState = OrderState.Placed;
                    Data.OrderId = orderIds.Dequeue();
                }

                // call out to somewhere else to request that the order is billed
            }

            public void Handle(OrderBilled message)
            {
                Assert.AreEqual(OrderState.Placed, Data.OrderState);

                Data.OrderState = OrderState.Billed;
            }

            public void Handle(PaymentReceived message)
            {
                Assert.AreEqual(OrderState.Billed, Data.OrderState);

                Data.OrderState = OrderState.PaymentReceived;
            }
        }

        class OrderPlaced
        {
            public Guid OrderPlacementId { get; set; }
            public string ProductName { get; set; }
        }

        class OrderBilled
        {
            public Guid OrderId { get; set; }
        }

        class PaymentReceived
        {
            public Guid OrderId { get; set; }
        }
    }
}