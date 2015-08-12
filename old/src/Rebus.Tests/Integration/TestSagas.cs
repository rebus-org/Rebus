using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Persistence.InMemory;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestSagas : FixtureBase
    {
        Dispatcher dispatcher;
        SagaDataPersisterForTesting persister;
        HandlerActivatorForTesting activator;

        protected override void DoSetUp()
        {
            persister = new SagaDataPersisterForTesting();
            activator = new HandlerActivatorForTesting();
            dispatcher = new Dispatcher(persister, activator,
                                        new InMemorySubscriptionStorage(),
                                        new TrivialPipelineInspector(),
                                        new DeferredMessageHandlerForTesting(),
                                        null);
        }

        [Test]
        public void CanHandleMultipleCorrelations()
        {
            var orderNumbers = new Queue<int>();
            orderNumbers.Enqueue(1000);
            orderNumbers.Enqueue(1001);
            activator.UseHandler(() => new MySaga(orderNumbers));

            dispatcher.Dispatch(new OrderPlaced { OrderPlacementId = 2, ProductName = "beer" });
            dispatcher.Dispatch(new OrderPlaced { OrderPlacementId = 3, ProductName = "nuts" });
            dispatcher.Dispatch(new OrderPlaced { OrderPlacementId = 2, ProductName = "beer" });

            Assert.AreEqual(2, persister.Count());
            Assert.IsTrue(persister.Cast<MySagaData>().All(s => s.State == OrderState.Placed));
            Assert.AreEqual(1000, persister.Cast<MySagaData>().Single(d => d.OrderPlacementId == 2).OrderNumber);
            Assert.AreEqual(1001, persister.Cast<MySagaData>().Single(d => d.OrderPlacementId == 3).OrderNumber);

            dispatcher.Dispatch(new OrderBilled{OrderNumber = 1001});

            Assert.AreEqual(OrderState.Placed, persister.Cast<MySagaData>().Single(d => d.OrderPlacementId == 2).State);
            Assert.AreEqual(OrderState.Billed, persister.Cast<MySagaData>().Single(d => d.OrderPlacementId == 3).State);

            dispatcher.Dispatch(new PaymentReceived { OrderNumber = 1001 });

            Assert.AreEqual(OrderState.Paid, persister.Cast<MySagaData>().Single(d => d.OrderPlacementId == 3).State);

            dispatcher.Dispatch(new OrderBilled { OrderNumber = 1000 });
            dispatcher.Dispatch(new PaymentReceived { OrderNumber = 1000 });

            Assert.AreEqual(OrderState.Paid, persister.Cast<MySagaData>().Single(d => d.OrderPlacementId == 2).State);

            dispatcher.Dispatch(new EvalutionCompleted {OrderNumber = 1000});
            
            Assert.AreEqual(1, persister.Count());
            Assert.AreEqual(1001, persister.Cast<MySagaData>().Single().OrderNumber);

            dispatcher.Dispatch(new EvalutionCompleted {OrderNumber = 1001});

            Assert.AreEqual(0, persister.Count());
        }

        class MySaga : Saga<MySagaData>,
            IAmInitiatedBy<OrderPlaced>,
            IHandleMessages<OrderBilled>,
            IHandleMessages<PaymentReceived>,
            IHandleMessages<EvalutionCompleted>
        {
            readonly Queue<int> orderNumbers;

            public MySaga(Queue<int> orderNumbers)
            {
                this.orderNumbers = orderNumbers;
            }

            public override void ConfigureHowToFindSaga()
            {
                Incoming<OrderPlaced>(m => m.OrderPlacementId).CorrelatesWith(d => d.OrderPlacementId);
                Incoming<OrderBilled>(m => m.OrderNumber).CorrelatesWith(d => d.OrderNumber);
                Incoming<PaymentReceived>(m => m.OrderNumber).CorrelatesWith(d => d.OrderNumber);
                Incoming<EvalutionCompleted>(m => m.OrderNumber).CorrelatesWith(d => d.OrderNumber);
            }

            public void Handle(OrderPlaced message)
            {
                Assert.IsTrue(Data.State == OrderState.Placed || Data.State == OrderState.Init);

                if (Data.State == OrderState.Init)
                {
                    Data.State = OrderState.Placed;
                    Data.ProductName = message.ProductName;
                    Data.OrderPlacementId = message.OrderPlacementId;
                    Data.OrderNumber = orderNumbers.Dequeue();
                }

                // bus.Send("billing", new BillOrder{Id = something bla bla})
            }

            public void Handle(OrderBilled message)
            {
                Assert.AreEqual(OrderState.Placed, Data.State);

                Data.State = OrderState.Billed;

                // bus.Send("timeout", new GetBackToMeIfHeDoesntPay{....)
            }

            public void Handle(PaymentReceived message)
            {
                Assert.AreEqual(OrderState.Billed, Data.State);

                Data.State = OrderState.Paid;
            }

            public void Handle(EvalutionCompleted message)
            {
                MarkAsComplete();
            }
        }

        enum OrderState
        {
            Init,
            Placed,
            Billed,
            Paid
        }

        class MySagaData : ISagaData
        {
            public MySagaData()
            {
                Id = Guid.NewGuid();
            }

            public Guid Id { get; set; }
            public int Revision { get; set; }
            public int OrderPlacementId { get; set; }
            public int OrderNumber { get; set; }
            public string ProductName { get; set; }
            public OrderState State { get; set; }
        }

        class OrderPlaced
        {
            public int OrderPlacementId { get; set; }
            public string ProductName { get; set; }
        }

        class OrderBilled
        {
            public int OrderNumber { get; set; }
        }

        class PaymentReceived
        {
            public int OrderNumber { get; set; }
        }

        class EvalutionCompleted
        {
            public int OrderNumber { get; set; }
        }
    }
}