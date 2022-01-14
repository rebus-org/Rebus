using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.DataBus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Routing;
using Rebus.Time;
using Rebus.Transport;
// ReSharper disable ArgumentsStyleLiteral

namespace Rebus.Bus;

/// <summary>
/// The implementations of the advanced APIs are private classes inside <see cref="RebusBus"/> so that they can access private functions and stuff
/// </summary>
public partial class RebusBus
{
    class AdvancedApi : IAdvancedApi
    {
        readonly RebusBus _rebusBus;
        readonly IRebusTime _rebusTime;

        public AdvancedApi(RebusBus rebusBus, IRebusTime rebusTime)
        {
            _rebusBus = rebusBus;
            _rebusTime = rebusTime;
        }

        public IWorkersApi Workers => new WorkersApi(_rebusBus);

        public ITopicsApi Topics => new TopicsApi(_rebusBus);

        public IRoutingApi Routing => new RoutingApi(_rebusBus, _rebusTime);

        public ITransportMessageApi TransportMessage => new TransportMessageApi(_rebusBus, _rebusTime);

        public IDataBus DataBus => _rebusBus._dataBus;

        public ISyncBus SyncBus => new SyncApi(_rebusBus);
    }

    class TransportMessageApi : ITransportMessageApi
    {
        readonly RebusBus _rebusBus;
        readonly IRebusTime _rebusTime;

        public TransportMessageApi(RebusBus rebusBus, IRebusTime rebusTime)
        {
            _rebusBus = rebusBus ?? throw new ArgumentNullException(nameof(rebusBus));
            _rebusTime = rebusTime ?? throw new ArgumentNullException(nameof(rebusTime));
        }

        public async Task Forward(string destinationAddress, Dictionary<string, string> optionalAdditionalHeaders = null)
        {
            var transportMessage = GetCloneOfCurrentTransportMessage(optionalAdditionalHeaders);

            await _rebusBus.SendTransportMessage(destinationAddress, transportMessage);
        }

        public async Task Defer(TimeSpan delay, Dictionary<string, string> optionalAdditionalHeaders = null)
        {
            var transportMessage = GetCloneOfCurrentTransportMessage(optionalAdditionalHeaders);
            var timeoutManagerAddress = _rebusBus.GetTimeoutManagerAddress();

            transportMessage.SetDeferHeaders(_rebusTime.Now + delay, _rebusBus._transport.Address);

            await _rebusBus.SendTransportMessage(timeoutManagerAddress, transportMessage);
        }

        TransportMessage GetCloneOfCurrentTransportMessage(IDictionary<string, string> optionalAdditionalHeaders)
        {
            var transactionContext = _rebusBus.GetCurrentTransactionContext(mustBelongToThisBus: false);

            if (transactionContext == null)
            {
                throw new InvalidOperationException(
                    "Attempted to perform operation on the current transport message, but there was no transaction context and therefore no 'current transport message' to do anything with! This call must be made from within a message handler... if you're actually doing that, you're probably experiencing this error because you're executing this operation on an independent thread somehow...");
            }

            var originalTransportMessage = transactionContext
                .GetOrThrow<IncomingStepContext>(StepContext.StepContextKey)
                .Load<OriginalTransportMessage>();

            if (originalTransportMessage == null)
            {
                throw new InvalidOperationException(
                    "Attempted to perform operation on the current transport message, but no transport message could be found in the context.... this is odd because the entire receive pipeline will only get called when there is a transport message, so maybe it has somehow been removed?");
            }

            var currentTransportMessage = originalTransportMessage.TransportMessage;

            var headers = optionalAdditionalHeaders != null
                ? currentTransportMessage.Headers.MergedWith(optionalAdditionalHeaders)
                : currentTransportMessage.Headers.Clone();

            var body = currentTransportMessage.Body;
            var clone = new TransportMessage(headers, body);

            return clone;
        }
    }

    class SyncApi : ISyncBus
    {
        readonly RebusBus _rebusBus;

        public SyncApi(RebusBus rebusBus)
        {
            _rebusBus = rebusBus;
        }

        public void SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null)
        {
            AsyncHelpers.RunSync(() => _rebusBus.SendLocal(commandMessage, optionalHeaders));
        }

        public void Send(object commandMessage, IDictionary<string, string> optionalHeaders = null)
        {
            AsyncHelpers.RunSync(() => _rebusBus.Send(commandMessage, optionalHeaders));
        }

        public void Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null)
        {
            AsyncHelpers.RunSync(() => _rebusBus.Reply(replyMessage, optionalHeaders));
        }

        public void Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
        {
            AsyncHelpers.RunSync(() => _rebusBus.Defer(delay, message, optionalHeaders));
        }

        public void DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
        {
            AsyncHelpers.RunSync(() => _rebusBus.DeferLocal(delay, message, optionalHeaders));
        }

        public void Subscribe<TEvent>()
        {
            AsyncHelpers.RunSync(() => _rebusBus.Subscribe<TEvent>());
        }

        public void Subscribe(Type eventType)
        {
            AsyncHelpers.RunSync(() => _rebusBus.Subscribe(eventType));
        }

        public void Unsubscribe<TEvent>()
        {
            AsyncHelpers.RunSync(() => _rebusBus.Unsubscribe<TEvent>());
        }

        public void Unsubscribe(Type eventType)
        {
            AsyncHelpers.RunSync(() => _rebusBus.Unsubscribe(eventType));
        }

        public void Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null)
        {
            AsyncHelpers.RunSync(() => _rebusBus.Publish(eventMessage, optionalHeaders));
        }
    }

    class RoutingApi : IRoutingApi
    {
        readonly RebusBus _rebusBus;
        readonly IRebusTime _rebusTime;

        public RoutingApi(RebusBus rebusBus, IRebusTime rebusTime)
        {
            _rebusBus = rebusBus;
            _rebusTime = rebusTime;
        }

        public Task Send(string destinationAddress, object explicitlyRoutedMessage, IDictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(explicitlyRoutedMessage, Operation.Send, optionalHeaders);

            return _rebusBus.InnerSend(new[] { destinationAddress }, logicalMessage);
        }

        public Task Defer(string destinationAddress, TimeSpan delay, object explicitlyRoutedMessage, IDictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(explicitlyRoutedMessage, Operation.Defer, optionalHeaders);

            logicalMessage.SetDeferHeaders(_rebusTime.Now + delay, destinationAddress);

            var timeoutManagerAddress = _rebusBus.GetTimeoutManagerAddress();

            return _rebusBus.InnerSend(new[] { timeoutManagerAddress }, logicalMessage);
        }

        public Task SendRoutingSlip(Itinerary itinerary, object message, IDictionary<string, string> optionalHeaders = null)
        {
            var logicalMessage = CreateMessage(message, Operation.Send, optionalHeaders);
            var destinationAddresses = itinerary.GetDestinationAddresses();

            if (!destinationAddresses.Any())
            {
                throw new ArgumentException($"Cannot send routing slip {message} because the itinerary does not contain any destination addresses. The itinerary must contain at least one destination, otherwise Rebus does not know where to send the message");
            }

            if (itinerary.MustReturnToSender)
            {
                var ownAddress = _rebusBus._transport.Address;
                if (string.IsNullOrWhiteSpace(ownAddress))
                {
                    throw new InvalidOperationException($"The itinerary if the routing slip {message} says to return it to the sender when done, but the sender appears to be a one-way client (and thus is not capable of receiving anything). When one-way clients send routing slips, they must either send them along their way without specifying a return address, or they must explicitly specify a return address by using the itinerary.ReturnTo(...) method");
                }
                destinationAddresses.Add(ownAddress);
            }
            else if (itinerary.HasExplicitlySpecifiedReturnAddress)
            {
                destinationAddresses.Add(itinerary.GetReturnAddress);
            }

            var first = destinationAddresses.First();
            var rest = destinationAddresses.Skip(1);

            var value = string.Join(";", rest);

            logicalMessage.Headers[Headers.RoutingSlipItinerary] = value;
            logicalMessage.Headers[Headers.RoutingSlipTravelogue] = "";


            return _rebusBus.InnerSend(new[] { first }, logicalMessage);
        }
    }

    class WorkersApi : IWorkersApi
    {
        readonly RebusBus _rebusBus;

        public WorkersApi(RebusBus rebusBus)
        {
            _rebusBus = rebusBus;
        }

        public int Count => _rebusBus.GetNumberOfWorkers();

        public void SetNumberOfWorkers(int numberOfWorkers)
        {
            _rebusBus.SetNumberOfWorkers(numberOfWorkers);
        }
    }

    class TopicsApi : ITopicsApi
    {
        readonly RebusBus _rebusBus;

        public TopicsApi(RebusBus rebusBus)
        {
            _rebusBus = rebusBus;
        }

        public Task Publish(string topic, object eventMessage, IDictionary<string, string> optionalHeaders = null)
        {
            return _rebusBus.InnerPublish(topic, eventMessage, optionalHeaders);
        }

        public Task Subscribe(string topic)
        {
            return _rebusBus.InnerSubscribe(topic);
        }

        public Task Unsubscribe(string topic)
        {
            return _rebusBus.InnerUnsubscribe(topic);
        }
    }
}