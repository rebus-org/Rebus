using System.Threading.Tasks;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Contracts.Activation
{
    public class RealContainerTests<TFactory> : FixtureBase where TFactory : IContainerAdapterFactory, new()
    {
        TFactory _factory;

        public RealContainerTests()
        {
            _factory = new TFactory();
        }

        [Fact]
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

            Assert.True(HandlerThatGetsMessageContextInjected.MessageContextWasInjected,
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