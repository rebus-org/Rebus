using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Castle.Windsor;
using Rebus.Configuration;
using Rebus.Persistence.InMemory;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    public class WindsorContainerAdapterDoesNotReleaseCorrectly
    {   
        [Test]
        public void HandlersAreReleasesCorrectly()
        {
            var c = new WindsorContainer();
            c.Register(Component.For<IHandleMessages<Message>>().ImplementedBy<Handler>().LifeStyle.Transient);
            
            var activator = new WindsorContainerAdapter(c);
            var pipelineInspector = new RearrangeHandlersPipelineInspector();
            var dispatcher = new Dispatcher(new InMemorySagaPersister(),
                                        activator,
                                        new InMemorySubscriptionStorage(),
                                        pipelineInspector,
                                        new DeferredMessageHandlerForTesting(),
                                        null);


            var message = new Message();
            dispatcher.Dispatch(message);
            message.MyHandlerWasDisposed.ShouldBe(true);
        }

        public class Message
        {
            public bool MyHandlerWasDisposed { get; set; }
        }

        public class Handler : IHandleMessages<Message>, IDisposable
        {
            Message message;

            public void Dispose()
            {
                message.MyHandlerWasDisposed = true;
            }

            public void Handle(Message message)
            {
                this.message = message;
            }
        }

        public class IWasDisposed : Exception
        {
        }
    }

}