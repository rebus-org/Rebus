using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Tests;
using Rebus.Tests.Contracts;
using SimpleInjector;

namespace Rebus.SimpleInjector.Tests
{
    [TestFixture]
    public class TestContainerVerification : FixtureBase
    {
        [Test]
        [Description("Verifies that auto-registered Rebus components (like IMessageContext, maybe other?) can be resolved as part of SimpleInjector's verification process")]
        public void CanVerifyContainer()
        {
            var container = new Container();
            var adapter = new SimpleInjectorContainerAdapter(container);
            adapter.SetBus(new FakeBus());

            container.Verify();
        }

        class FakeBus: IBus
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }

            public IAdvancedApi Advanced { get; }
            public Task Subscribe<TEvent>()
            {
                throw new NotImplementedException();
            }

            public Task Subscribe(Type eventType)
            {
                throw new NotImplementedException();
            }

            public Task Unsubscribe<TEvent>()
            {
                throw new NotImplementedException();
            }

            public Task Unsubscribe(Type eventType)
            {
                throw new NotImplementedException();
            }

            public Task Publish(object eventMessage, Dictionary<string, string> optionalHeaders = null)
            {
                throw new NotImplementedException();
            }
        }
    }
}