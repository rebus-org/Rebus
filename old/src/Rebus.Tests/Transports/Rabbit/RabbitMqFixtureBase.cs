using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RabbitMQ.Client;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.RabbitMQ;
using log4net.Config;

namespace Rebus.Tests.Transports.Rabbit
{
    public abstract class RabbitMqFixtureBase : IDetermineMessageOwnership
    {
        public const string ConnectionString = "amqp://guest:guest@localhost";

        protected readonly List<string> queuesToDelete = new List<string>();

        static RabbitMqFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            queuesToDelete.ForEach(DeleteQueue);
            DoTearDown();
            CleanUpTrackedDisposables();
            RebusLoggerFactory.Reset();
        }

        protected virtual void DoTearDown()
        {
        }

        protected void CleanUpTrackedDisposables()
        {
            DisposableTracker.DisposeTheDisposables();
        }

        protected T TrackDisposable<T>(T disposable) where T : IDisposable
        {
            DisposableTracker.TrackDisposable(disposable);
            return disposable;
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator)
        {
            queuesToDelete.Add(inputQueueName);
            queuesToDelete.Add(inputQueueName + ".error");


            var bus = Configure.With(new FakeContainerAdapter(handlerActivator))
                               .Transport(x => x.UseRabbitMq(ConnectionString, inputQueueName, inputQueueName + ".error"))
                               .CreateBus() as RebusBus;

            var rabbitMqMessageQueue = new RabbitMqMessageQueue(ConnectionString, inputQueueName).PurgeInputQueue();

            DisposableTracker.TrackDisposable(rabbitMqMessageQueue);
            DisposableTracker.TrackDisposable(bus);

            return bus;
        }

        public virtual string GetEndpointFor(Type messageType)
        {
            throw new NotImplementedException(string.Format("Don't know the destination of {0} - override this method in derived classes", messageType));
        }

        public static void DeleteQueue(string queueName)
        {
            using (var connection = new ConnectionFactory { Uri = ConnectionString }.CreateConnection())
            using (var model = connection.CreateModel())
            {
                // just ignore if it fails...
                try
                {
                    model.QueueDelete(queueName);
                }
                catch
                {
                }
            }
        }

        public static void WithModel(Action<IModel> modelCallback)
        {
            using (var connection = new ConnectionFactory {Uri = ConnectionString}.CreateConnection())
            using (var model = connection.CreateModel())
            {
                modelCallback(model);
            }
        }

        public static bool DeclareExchange(string exchangeName, string type, bool passive=false)
        {
            using (var connection = new ConnectionFactory { Uri = ConnectionString }.CreateConnection())
            using (var model = connection.CreateModel())
            {
                // just ignore if it fails...
                try
                {
                    if (passive)
                    {
                        model.ExchangeDeclarePassive(exchangeName);
                    }
                    else
                    {
                        model.ExchangeDeclare(exchangeName, type);
                    }
                }
                catch
                {
                    return false;
                }

                return true;
            }
        }

        public static void DeleteExchange(string exchangeName)
        {
            using (var connection = new ConnectionFactory { Uri = ConnectionString }.CreateConnection())
            using (var model = connection.CreateModel())
            {
                // just ignore if it fails...
                try
                {
                    model.ExchangeDelete(exchangeName);
                }
                catch
                {
                }
            }
        }

        public static bool DeclareQueue(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, bool passive = false)
        {
            using (var connection = new ConnectionFactory { Uri = ConnectionString }.CreateConnection())
            using (var model = connection.CreateModel())
            {
                // just ignore if it fails...
                try
                {
                    if (passive)
                    {
                        model.QueueDeclarePassive(queueName);
                    }
                    else
                    {
                        model.QueueDeclare(queueName, durable, exclusive, autoDelete, null);
                    }


                }
                catch
                {
                    return false;
                }

                return true;
            }
        }


        class FakeContainerAdapter : IContainerAdapter
        {
            readonly IActivateHandlers handlerActivator;

            public FakeContainerAdapter(IActivateHandlers handlerActivator)
            {
                this.handlerActivator = handlerActivator;
            }

            public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
            {
                return handlerActivator.GetHandlerInstancesFor<T>();
            }

            public void Release(IEnumerable handlerInstances)
            {
                handlerActivator.Release(handlerInstances);
            }

            public void SaveBusInstances(IBus bus)
            {
            }
        }
    }
}
