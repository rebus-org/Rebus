using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus.Advanced;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

// ReSharper disable CheckNamespace

namespace Rebus.Bus
{
    // private classes with access to private members of RebusBus
    public partial class RebusBus
    {
        class AdvancedApi : IAdvancedApi
        {
            readonly RebusBus _rebusBus;

            public AdvancedApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public IWorkersApi Workers
            {
                get { return new WorkersApi(_rebusBus); }
            }

            public ITopicsApi Topics
            {
                get { return new TopicsApi(_rebusBus); }
            }

            public IRoutingApi Routing
            {
                get { return new RoutingApi(_rebusBus); }
            }
        }

        class RoutingApi : IRoutingApi
        {
            readonly RebusBus _rebusBus;

            public RoutingApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public Task Send(string destinationAddress, object explicitlyRoutedMessage, Dictionary<string, string> optionalHeaders = null)
            {
                var logicalMessage = CreateMessage(explicitlyRoutedMessage, Operation.Send, optionalHeaders);

                return  _rebusBus.InnerSend(new[] { destinationAddress }, logicalMessage);
            }

            public async Task Forward(string destinationAddress, Dictionary<string, string> optionalAdditionalHeaders = null)
            {
                var transactionContext = AmbientTransactionContext.Current;

                if (transactionContext == null)
                {
                    throw new InvalidOperationException("Attempted to forward current transport message, but there was no transaction context!");
                }

                var currentTransportMessage = transactionContext
                    .GetOrThrow<IncomingStepContext>(StepContext.StepContextKey)
                    .Load<TransportMessage>();

                if (currentTransportMessage == null)
                {
                    throw new InvalidOperationException("Attempted to forward current transport message, but no transport message was found in the context!");
                }

                var headers = currentTransportMessage.Headers.Clone();

                if (optionalAdditionalHeaders != null)
                {
                    foreach (var kvp in optionalAdditionalHeaders)
                    {
                        headers[kvp.Key] = kvp.Value;
                    }
                }

                var body = currentTransportMessage.Body;
                var forwardedTransportMessage = new TransportMessage(headers, body);

                await _rebusBus.SendTransportMessage(destinationAddress, forwardedTransportMessage, transactionContext);
            }
        }

        class WorkersApi : IWorkersApi
        {
            readonly RebusBus _rebusBus;

            public WorkersApi(RebusBus rebusBus)
            {
                _rebusBus = rebusBus;
            }

            public int Count
            {
                get { return _rebusBus.GetNumberOfWorkers(); }
            }

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

            public Task Publish(string topic, object eventMessage, Dictionary<string, string> optionalHeaders = null)
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
}