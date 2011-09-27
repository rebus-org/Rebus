using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;

namespace Rebus.Tests.Integration
{
    [TestFixture, Category(TestCategories.Integration)]
    public class TestRebusBusWithMsmqMessageQueue
    {
        [Test]
        public void CanSendAndReceiveMessagesLikeExpected()
        {
            var recipientWasCalled = false;
            var senderQueueName = PrivateQueueNamed("test.sender");
            var recipientQueueName = PrivateQueueNamed("test.recipient");

            var manualResetEvent = new ManualResetEvent(false);

            var senderBus = GetBus(senderQueueName, str => { });
            
            GetBus(recipientQueueName, str =>
                                           {
                                               recipientWasCalled = true;
                                               manualResetEvent.Set();
                                           });
            
            senderBus.Send(recipientQueueName, "yo!");

            manualResetEvent.WaitOne(TimeSpan.FromSeconds(5));

            Assert.IsTrue(recipientWasCalled, "The recipient did not receive a call within allotted timeout");
        }

        static RebusBus GetBus(string senderQueueName, Action<string> handlerMethod)
        {
            var testHandlerFactory = new TestHandlerFactory();
            testHandlerFactory.Handle(handlerMethod);
            
            var testMessageTypeProvider = new TestMessageTypeProvider();
            var messageQueue = new MsmqMessageQueue(senderQueueName, testMessageTypeProvider);
            
            return new RebusBus(testHandlerFactory, messageQueue, messageQueue)
                .Start();
        }

        string PrivateQueueNamed(string queueName)
        {
            return string.Format(@".\private$\{0}", queueName);
        }
    }

    public class TestMessageTypeProvider : IProvideMessageTypes
    {
        public Type[] GetMessageTypes()
        {
            return new[] {typeof (string)};
        }
    }

    public class TestHandlerFactory : IHandlerFactory
    {
        readonly List<object> handlers = new List<object>();

        class HandlerMethodWrapper<T> : IHandleMessages<T>
        {
            readonly Action<T> action;

            public HandlerMethodWrapper(Action<T> action)
            {
                this.action = action;
            }

            public void Handle(T message)
            {
                action(message);
            }
        }

        public void Handle<T>(Action<T> handlerMethod)
        {
            handlers.Add(new HandlerMethodWrapper<T>(handlerMethod));
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            return handlers
                .Where(h => h is IHandleMessages<T>)
                .Cast<IHandleMessages<T>>()
                .ToList();
        }

        public void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances)
        {
        }
    }
}