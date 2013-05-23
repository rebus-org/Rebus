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

        protected List<IDisposable> toDispose;
        protected readonly List<string> queuesToDelete = new List<string>();

        static RabbitMqFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            toDispose = new List<IDisposable>();
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            toDispose.ForEach(b =>
                {
                    try
                    {
                        b.Dispose();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            queuesToDelete.ForEach(DeleteQueue);
            DoTearDown();
            RebusLoggerFactory.Reset();
        }

        protected virtual void DoTearDown()
        {
        }

        protected RebusBus CreateBus(string inputQueueName, IActivateHandlers handlerActivator)
        {
            queuesToDelete.Add(inputQueueName);
            queuesToDelete.Add(inputQueueName + ".error");


            RabbitMqMessageQueue rabbitMqMessageQueue =
                new RabbitMqMessageQueue(ConnectionString, inputQueueName)
                    .PurgeInputQueue();
            var bus = Configure.With(new FakeContainerAdapter(handlerActivator))
                               .Transport(x => x.UseRabbitMq(ConnectionString, inputQueueName, inputQueueName + ".error"))
                               .CreateBus() as RebusBus;

            toDispose.Add(bus);
            toDispose.Add(rabbitMqMessageQueue);

            return bus;
        }

        public virtual string GetEndpointFor(Type messageType)
        {
            throw new NotImplementedException(string.Format("Don't know the destination of {0} - override this method in derived classes", messageType));
        }

        public static void DeleteQueue(string queueName)
        {
            using (var connection = new ConnectionFactory {Uri = ConnectionString}.CreateConnection())
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

        class FakeContainerAdapter : IContainerAdapter
        {
            readonly IActivateHandlers handlerActivator;

            public FakeContainerAdapter(IActivateHandlers handlerActivator)
            {
                this.handlerActivator = handlerActivator;
            }

            public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
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