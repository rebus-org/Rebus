using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Pipeline.Send;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;
using Rebus.Transport.InMem;

namespace Rebus.Tests.Examples;

[TestFixture]
public class AddSagaFieldToOutgoingMessages : FixtureBase
{
    [Test]
    public async Task HereIsHowToDoIt()
    {
        var network = new InMemNetwork();

        network.CreateQueue("forwardedmessagereceiver");

        using var activator = new BuiltinHandlerActivator();

        activator.Register((bus, context) => new MySaga(bus));

        Configure.With(activator)
            .Transport(t => t.UseInMemoryTransport(network, "sagaendpoint"))
            .Sagas(s => s.StoreInMemory())
            .Routing(r => r.TypeBased().Map<ForwardedMessage>("forwardedmessagereceiver"))
            .Options(o => o.Decorate<IPipeline>(c =>
            {
                var pipeline = c.Get<IPipeline>();
                var step = new SetOrderIdOnOutgoingMessages();
                return new PipelineStepInjector(pipeline)
                    // inject step before serialization so we can add the header to Message
                    .OnSend(step, PipelineRelativePosition.Before, typeof(SerializeOutgoingMessageStep));
            }))
            .Start();

        var orderId = Guid.NewGuid().ToString();

        await activator.Bus.SendLocal(new MessageWithOrderId(orderId));

        var transportMessage = await network.WaitForNextMessageFrom("forwardedmessagereceiver");

        Assert.That(transportMessage.Headers, Contains.Key("some-data"));
        Assert.That(transportMessage.Headers["some-data"], Is.EqualTo(orderId));
    }

    interface ISagaDataWithOrderId
    {
        string OrderId { get; }
    }

    class MySagaData : SagaData, ISagaDataWithOrderId
    {
        public string OrderId { get; set; }
    }

    record MessageWithOrderId(string OrderId);

    record ForwardedMessage;

    class MySaga : Saga<MySagaData>, IAmInitiatedBy<MessageWithOrderId>
    {
        readonly IBus _bus;

        public MySaga(IBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        protected override void CorrelateMessages(ICorrelationConfig<MySagaData> config)
        {
            config.Correlate<MessageWithOrderId>(m => m.OrderId, s => s.OrderId);
        }

        public async Task Handle(MessageWithOrderId message)
        {
            Data.OrderId = message.OrderId;

            // magically transfer order ID to outgoing message
            await _bus.Send(new ForwardedMessage());
        }
    }

    class SetOrderIdOnOutgoingMessages : IOutgoingStep
    {
        public async Task Process(OutgoingStepContext context, Func<Task> next)
        {
            var messageContext = MessageContext.Current;

            // detect if we're currently handling a message
            if (messageContext != null)
            {
                var incomingStepContext = messageContext.IncomingStepContext;
                var handlerInvokers = incomingStepContext.Load<HandlerInvokers>();

                // detect if we have a saga handler
                if (handlerInvokers.Count == 1)
                {
                    var handlerInvoker = handlerInvokers.First();

                    if (handlerInvoker.HasSaga)
                    {
                        // detect if saga data is carrying an order ID
                        if (handlerInvoker.GetSagaData() is ISagaDataWithOrderId sagaDataWithOrderId)
                        {
                            // ...and if so, add it as a header on the outgoing message
                            var orderId = sagaDataWithOrderId.OrderId;
                            var outgoingMessage = context.Load<Message>();
                            outgoingMessage.Headers["some-data"] = orderId;
                        }
                    }
                }
            }

            await next();
        }
    }
}