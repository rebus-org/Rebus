using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Contracts.Activation
{
    public class RealContainerTests<TFactory> : FixtureBase where TFactory : IContainerAdapterFactory, new()
    {
        TFactory _factory;

        protected override void SetUp()
        {
            _factory = new TFactory();
        }

        [Test]
        public async Task CanInjectMessageContext()
        {
            HandlerThatGetsMessageContextInjected.MessageContextWasInjected = false;
            _factory.RegisterHandlerType<HandlerThatGetsMessageContextInjected>();

            var activator = _factory.GetActivator();

            var bus = Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "hvassåbimse"))
                .Start();

            Using(bus);

            await bus.SendLocal("hej");

            await Task.Delay(500);

            Assert.That(HandlerThatGetsMessageContextInjected.MessageContextWasInjected, Is.True,
                "HandlerThatGetsMessageContextInjected did not get invoked properly with an injected IMessageContext");
        }

        class HandlerThatGetsMessageContextInjected : IHandleMessages<string>
        {
            public static bool MessageContextWasInjected;

            readonly IMessageContext _messageContext;

            public HandlerThatGetsMessageContextInjected(IMessageContext messageContext)
            {
                _messageContext = messageContext;
            }

            public async Task Handle(string message)
            {
                if (_messageContext != null)
                {
                    MessageContextWasInjected = true;
                }
            }
        }
    }
}