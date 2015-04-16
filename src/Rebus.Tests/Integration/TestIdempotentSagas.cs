using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.IdempotentSagas;
using Rebus.Persistence.InMemory;
using Rebus.RabbitMQ;
using Rebus.Tests.Transports.Rabbit;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    [Category(TestCategories.Rabbit)]
    public class TestIdempotentSagas : RabbitMqFixtureBase
    {
        BuiltinContainerAdapter adapter;
        IBus bus;
        const string QueueName = "test.idempotentSaga.input1";
        CanFailSagaStorage sagaStorage;

        protected override void DoSetUp()
        {
            DeleteQueue(QueueName);

            adapter = new BuiltinContainerAdapter();
            sagaStorage = new CanFailSagaStorage();

            bus = Configure.With(adapter)
                     .Transport(t => t.UseRabbitMq(ConnectionString, QueueName, "error"))
                     .Sagas(x => {
                         x.Use(sagaStorage);
                         x.WithIdempotentSagas();
                     })
                     .MessageOwnership(o => o.Use(this))
                     .CreateBus()
                     .Start();

            adapter.Bus = bus;
            MyIdempotentSaga.bus = bus;
        }

        protected override void DoTearDown()
        {
            adapter.Dispose();

            DeleteQueue(QueueName);
        }

        public class DummyMessage
        {
            public Guid Id { get; set; }
            public string Message { get; set; }
        }

        [Test]
        public void HandlersWorksAsUsual()
        {
            // Arrange
            var handled = false;
            var resetEvent = new ManualResetEvent(false);
            DeclareQueue(QueueName);

            adapter.Handle<DummyMessage>(req =>
            {
                handled = true;
                resetEvent.Set();
            });

            // Act
            adapter.Bus.Send(new DummyMessage { Message = "hello there!" });

            if (!resetEvent.WaitOne(3.Seconds()))
            {
                Assert.Fail("Did not receive message within 3 seconds of waiting!");
            }

            // Assert
            Assert.IsTrue(handled);
        }

        public class MySagaData : ISagaData
        {
            public Guid Id { get; set; }

            public int Revision { get; set; }
        }

        public class MySaga : Saga<MySagaData>, IAmInitiatedBy<DummyMessage>
        {
            public static ManualResetEvent MyHandle;
            public override void ConfigureHowToFindSaga() { }

            public void Handle(DummyMessage message) 
            {
                MyHandle.Set();
            }
        }

        public class MyIdempotentSagaData : IIdempotentSagaData
        {
            public Guid Id { get; set; }

            public int Revision { get; set; }

            public Guid Identifier { get; set; }

            public IList<IdempotentSagaResults> ExecutionResults { get; set; }
        }

        public class MyIdempotentSaga : IdempotentSaga<MyIdempotentSagaData>, IAmInitiatedBy<DummyMessage>
            , IHandleMessages<MessageSentBySaga>
        {
            public static ManualResetEvent FirstHandle;
            public static ManualResetEvent SecondHandle;
            public static IBus bus;
            public static int TimesDummyMessageHandlerExecuted = 0;

            public override void ConfigureHowToFindSaga() 
            {
                Incoming<DummyMessage>(m => m.Id).CorrelatesWith(s => s.Identifier);
                Incoming<MessageSentBySaga>(m => m.Id).CorrelatesWith(s => s.Identifier);
            }

            public void Handle(DummyMessage message)
            {
                Data.Identifier = message.Id;
                TimesDummyMessageHandlerExecuted++;
                if (FirstHandle != null)
                    FirstHandle.Set();
                bus.Send(new MessageSentBySaga()
                    {
                        Id = message.Id
                    });
            }

            public void Handle(MessageSentBySaga message)
            {
                if (SecondHandle != null)
                    SecondHandle.Set();
            }
        }

        [Test]
        public void SagasWorksAsUsual()
        {
            // Arrange
            var resetEvent = new ManualResetEvent(false);
            DeclareQueue(QueueName);
            MySaga.MyHandle = resetEvent;
            adapter.Register(typeof(MySaga));

            // Act
            adapter.Bus.Send(new DummyMessage 
            { 
                Message = "hello there!"
            });

            // Assert
            if (!resetEvent.WaitOne(3.Seconds()))
            {
                Assert.Fail("Did not receive message within 3 seconds of waiting!");
            }
        }

        [Test]
        public void IdempotentSagaWorkAsUsual()
        {
            // Arrange
            var resetEvent = new ManualResetEvent(false);
            DeclareQueue(QueueName);
            MyIdempotentSaga.FirstHandle = resetEvent;
            adapter.Register(typeof(MyIdempotentSaga));

            // Act
            adapter.Bus.Send(new DummyMessage
            {
                Message = "hello there!"
            });

            // Assert
            if (!resetEvent.WaitOne(3.Seconds()))
            {
                Assert.Fail("Did not receive message within 3 seconds of waiting!");
            }
        }

        public class CanFailSagaStorage : IStoreSagaData
        {
            private IStoreSagaData _inner = new InMemorySagaPersister();

            public bool ThrowAfterModification { get; set; }

            public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                _inner.Insert(sagaData, sagaDataPropertyPathsToIndex);

                if (ThrowAfterModification)
                {
                    ThrowAfterModification = false;
                    throw new Exception("Ohhh something failed!");
                }
            }

            public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
            {
                _inner.Update(sagaData, sagaDataPropertyPathsToIndex);

                if (ThrowAfterModification)
                {
                    ThrowAfterModification = false;
                    throw new Exception("Ohhh something failed!");
                }
            }

            public void Delete(ISagaData sagaData)
            {
                _inner.Delete(sagaData);

                if (ThrowAfterModification)
                {
                    ThrowAfterModification = false;
                    throw new Exception("Ohhh something failed!");
                }
            }

            public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : class, ISagaData
            {
                return _inner.Find<T>(sagaDataPropertyPath, fieldFromMessage);
            }
        }

        public class MessageSentBySaga
        {
            public Guid Id { get; set; }
        }
        
        [Test]
        public void IdempotentSagaReplaysSentMessagesWhenReprocessed()
        {
            // Arrange
            var resetEvent = new ManualResetEvent(false);
            DeclareQueue(QueueName);
            MyIdempotentSaga.SecondHandle = resetEvent;
            MyIdempotentSaga.TimesDummyMessageHandlerExecuted = 0;
            adapter.Register(typeof(MyIdempotentSaga));

            // Set the saga storage to simulate an exception after saving.
            sagaStorage.ThrowAfterModification = true;

            // Act
            bus.Send(new DummyMessage
            {
                Id = Guid.NewGuid(),
                Message = "hello there!"
            });

            // Assert
            if (!resetEvent.WaitOne(10.Seconds()))
            {
                Assert.Fail("Did not receive message within 10 seconds of waiting!");
            }

            Assert.AreEqual(1, MyIdempotentSaga.TimesDummyMessageHandlerExecuted);
        }

        public override string GetEndpointFor(Type messageType)
        {
            return QueueName;
        }

        [Test]
        public void IdempotentSagaMultipleHandlersWorksWithoutProblem()
        {
            // Arrange
            var sagaResetEvent = new ManualResetEvent(false);
            var handlerResetEvent = new ManualResetEvent(false);
            DeclareQueue(QueueName);
            MyIdempotentSaga.FirstHandle = sagaResetEvent;
            adapter.Register(typeof(MyIdempotentSaga));
            adapter.Handle<DummyMessage>(req =>
            {
                handlerResetEvent.Set();
            });

            // Act
            adapter.Bus.Send(new DummyMessage
            {
                Message = "hello there!"
            });

            // Assert
            if (!WaitHandle.WaitAll(new WaitHandle[] { sagaResetEvent, handlerResetEvent }, 10.Seconds()))
            {
                Assert.Fail("Did not receive message within 10 seconds of waiting!");
            }
        }

    }
}
