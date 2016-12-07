using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Handlers.Reordering;
using Rebus.Pipeline;
using Rebus.Routing.TransportMessages;
using Rebus.Tests.Contracts;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Pipeline
{
    public class TestAbortMessagePipeline : FixtureBase
    {
        BuiltinHandlerActivator _activator;
        ConcurrentQueue<string> _events;

        bool _shouldAbortPipelineInTransportMessageRoutingFilter;

        public TestAbortMessagePipeline()
        {
            _shouldAbortPipelineInTransportMessageRoutingFilter = false;

            _activator = Using(new BuiltinHandlerActivator());

            _events = new ConcurrentQueue<string>();

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "test abort pipeline"))
                .Options(o => o.SpecifyOrderOfHandlers()
                    .First<FirstHandler>()
                    .Then<SecondHandler>())
                .Routing(r => r.AddTransportMessageForwarder(async transportMessage =>
                {
                    if (_shouldAbortPipelineInTransportMessageRoutingFilter)
                    {
                        MessageContext.Current.AbortDispatch();
                    }

                    return ForwardAction.None;
                }))
                .Start();
        }

        [Fact]
        public async Task CanAbortMessageProcessingBeforeTheHandlers()
        {
            _shouldAbortPipelineInTransportMessageRoutingFilter = true;

            _activator.Register(context => new FirstHandler(_events, context, false));
            _activator.Register(() => new SecondHandler(_events));

            await _activator.Bus.SendLocal("hej med dig!!!");

            await Task.Delay(1000);

            Assert.Equal(new string[0], _events.ToArray());
        }

        [Fact]
        public async Task CanAbortMessageProcessing()
        {
            _activator.Register(context => new FirstHandler(_events, context, true));
            _activator.Register(() => new SecondHandler(_events));

            await _activator.Bus.SendLocal("hej med dig!!!");

            await Task.Delay(1000);

            Assert.Equal(new[] {"FirstHandler"}, _events.ToArray());
        }

        [Fact]
        public async Task CanAlsoNotAbortMessageProcessing()
        {
            _activator.Register(context => new FirstHandler(_events, context, false));
            _activator.Register(() => new SecondHandler(_events));

            await _activator.Bus.SendLocal("hej med dig!!!");

            await Task.Delay(1000);

            Assert.Equal(new[] { "FirstHandler", "SecondHandler" },_events.ToArray());
        }

        class FirstHandler : IHandleMessages<string>
        {
            readonly ConcurrentQueue<string> _events;
            readonly IMessageContext _messageContext;
            readonly bool _shouldAbortThisTime;

            public FirstHandler(ConcurrentQueue<string>  events, IMessageContext messageContext, bool shouldAbortThisTime)
            {
                _events = events;
                _messageContext = messageContext;
                _shouldAbortThisTime = shouldAbortThisTime;
            }

            public async Task Handle(string message)
            {
                Console.WriteLine("handling {0}", message);
                _events.Enqueue("FirstHandler");

                if (_shouldAbortThisTime)
                {
                    _messageContext.AbortDispatch();
                }
            }
        }

        class SecondHandler : IHandleMessages<string>
        {
            readonly ConcurrentQueue<string> _events;

            public SecondHandler(ConcurrentQueue<string>  events)
            {
                _events = events;
            }

            public async Task Handle(string message)
            {
                Console.WriteLine("handling {0}", message);
                _events.Enqueue("SecondHandler");
            }
        }
    }
}