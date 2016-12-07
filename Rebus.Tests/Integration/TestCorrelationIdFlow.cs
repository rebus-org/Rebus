using System.Collections.Generic;
using System.Linq;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Utilities;
using Rebus.Transport.InMem;
using Xunit;

#pragma warning disable 1998

namespace Rebus.Tests.Integration
{
    public class TestCorrelationIdFlow : FixtureBase
    {
        readonly InMemNetwork _network = new InMemNetwork();

        BuiltinHandlerActivator _activator1;
        BuiltinHandlerActivator _activator2;
        BuiltinHandlerActivator _activator3;

        public TestCorrelationIdFlow()
        {
            _activator1 = new BuiltinHandlerActivator();
            _activator2 = new BuiltinHandlerActivator();
            _activator3 = new BuiltinHandlerActivator();

            Using(_activator1);
            Using(_activator2);
            Using(_activator3);

            CreateBus("bus1", _activator1);
            CreateBus("bus2", _activator2);
            CreateBus("bus3", _activator3);
        }

        [Fact]
        public void CorrelationSequenceIsIncremented()
        {
            var correlationSequenceNumbers = new List<int>();
            var counter = new SharedCounter(1);

            _activator1.Handle<string>(async (bus, ctx, str) =>
            {
                correlationSequenceNumbers.Add(int.Parse(ctx.Headers[Headers.CorrelationSequence]));
                await bus.Advanced.Routing.Send("bus2", "hej!");
            });
            _activator2.Handle<string>(async (bus, ctx, str) =>
            {
                correlationSequenceNumbers.Add(int.Parse(ctx.Headers[Headers.CorrelationSequence]));
                await bus.Advanced.Routing.Send("bus3", "hej!");
            });
            _activator3.Handle<string>(async (bus, ctx, str) =>
            {
                correlationSequenceNumbers.Add(int.Parse(ctx.Headers[Headers.CorrelationSequence]));
                counter.Decrement();
            });

            _activator1.Bus.SendLocal("heeeej!").Wait();

            counter.WaitForResetEvent();

            Assert.Equal(new[] { 0, 1, 2 }, correlationSequenceNumbers);
        }

        [Fact]
        public void CorrelationIdFlows()
        {
            var correlationIds = new List<string>();
            var counter = new SharedCounter(1);

            _activator1.Handle<string>(async (bus, ctx, str) =>
            {
                correlationIds.Add(ctx.Headers[Headers.CorrelationId]);
                await bus.Advanced.Routing.Send("bus2", "hej!");
            });
            _activator2.Handle<string>(async (bus, ctx, str) =>
            {
                correlationIds.Add(ctx.Headers[Headers.CorrelationId]);
                await bus.Advanced.Routing.Send("bus3", "hej!");
            });
            _activator3.Handle<string>(async (bus, ctx, str) =>
            {
                correlationIds.Add(ctx.Headers[Headers.CorrelationId]);
                counter.Decrement();
            });

            _activator1.Bus.SendLocal("heeeej!").Wait();

            counter.WaitForResetEvent();

            Assert.Equal(1, correlationIds.GroupBy(c => c).Count());
        }

        [Fact]
        public void CorrelationIdIsFirstMessageId()
        {
            var messageIds = new List<string>();
            var correlationIds = new List<string>();
            var counter = new SharedCounter(1);

            _activator1.Handle<string>(async (bus, ctx, str) =>
            {
                messageIds.Add(ctx.Headers[Headers.MessageId]);
                correlationIds.Add(ctx.Headers[Headers.CorrelationId]);
                await bus.Advanced.Routing.Send("bus2", "hej!");
            });
            _activator2.Handle<string>(async (bus, ctx, str) =>
            {
                messageIds.Add(ctx.Headers[Headers.MessageId]);
                correlationIds.Add(ctx.Headers[Headers.CorrelationId]);
                counter.Decrement();
            });

            _activator1.Bus.SendLocal("heeeej!").Wait();

            counter.WaitForResetEvent();

            var firstMessageId = messageIds.First();
            Assert.True(correlationIds.All(c => c == firstMessageId));
        }

        [Fact]
        public void MessageIdsAreDifferent()
        {
            var messageIds = new List<string>();
            var counter = new SharedCounter(1);

            _activator1.Handle<string>(async (bus, ctx, str) =>
            {
                messageIds.Add(ctx.Headers[Headers.MessageId]);
                await bus.Advanced.Routing.Send("bus2", "hej!");
            });
            _activator2.Handle<string>(async (bus, ctx, str) =>
            {
                messageIds.Add(ctx.Headers[Headers.MessageId]);
                counter.Decrement();
            });

            _activator1.Bus.SendLocal("heeeej!").Wait();

            counter.WaitForResetEvent();

            Assert.Equal(2, messageIds.GroupBy(i => i).Count());
        }

        void CreateBus(string queueName, BuiltinHandlerActivator activator)
        {
            Configure.With(activator)
                .Transport(t => t.UseInMemoryTransport(_network, queueName))
                .Start();
        }
    }
}