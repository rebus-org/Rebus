using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config;

namespace Rebus.Transport.Msmq
{
    /// <summary>
    /// Configuration extensions for the MSMQ transport
    /// </summary>
    public static class MsmqTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use MSMQ to transport messages, receiving messages from the specified <paramref name="inputQueueName"/>
        /// </summary>
        public static void UseMsmq(this StandardConfigurer<ITransport> configurer, string inputQueueName)
        {
            configurer.Register(context => new MsmqTransport(inputQueueName));
        }

        /// <summary>
        /// Configures Rebus to use MSMQ to transport messages as a one-way client (i.e. will not be able to receive any messages)
        /// </summary>
        public static void UseMsmqAsOneWayClient(this StandardConfigurer<ITransport> configurer)
        {
            configurer.Register(context => new MsmqTransport(null));

            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }
    }

    class OneWayClientBusDecorator : IBus
    {
        readonly IBus _innerBus;
        readonly AdvancedApiDecorator _advancedApiDecorator;

        public OneWayClientBusDecorator(IBus innerBus)
        {
            _innerBus = innerBus;
            _advancedApiDecorator = new AdvancedApiDecorator(_innerBus.Advanced);
        }

        public void Dispose()
        {
            _innerBus.Dispose();
        }

        public Task SendLocal(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            return _innerBus.SendLocal(commandMessage, optionalHeaders);
        }

        public Task Send(object commandMessage, Dictionary<string, string> optionalHeaders = null)
        {
            return _innerBus.Send(commandMessage, optionalHeaders);
        }

        public Task Reply(object replyMessage, Dictionary<string, string> optionalHeaders = null)
        {
            return _innerBus.Reply(replyMessage, optionalHeaders);
        }

        public Task Defer(TimeSpan delay, object message, Dictionary<string, string> optionalHeaders = null)
        {
            return _innerBus.Defer(delay, message, optionalHeaders);
        }

        public Task Route(string destinationAddress, object explicitlyRoutedMessage, Dictionary<string, string> optionalHeaders = null)
        {
            return _innerBus.Route(destinationAddress, explicitlyRoutedMessage, optionalHeaders);
        }

        public IAdvancedApi Advanced
        {
            get { return _advancedApiDecorator; }
        }

        public Task Subscribe<TEvent>()
        {
            return _innerBus.Subscribe<TEvent>();
        }

        public Task Unsubscribe<TEvent>()
        {
            return _innerBus.Unsubscribe<TEvent>();
        }

        public Task Publish(object eventMessage, Dictionary<string, string> optionalHeaders = null)
        {
            return _innerBus.Publish(eventMessage, optionalHeaders);
        }

        class AdvancedApiDecorator : IAdvancedApi
        {
            readonly IAdvancedApi _innerAdvancedApi;

            public AdvancedApiDecorator(IAdvancedApi innerAdvancedApi)
            {
                _innerAdvancedApi = innerAdvancedApi;
            }

            public IWorkersApi Workers
            {
                get { return new OneWayClientWorkersApi(); }
            }

            public ITopicsApi Topics
            {
                get { return _innerAdvancedApi.Topics; }
            }
        }

        class OneWayClientWorkersApi : IWorkersApi
        {
            public int Count
            {
                get { return 0; }
            }

            public void SetNumberOfWorkers(int numberOfWorkers)
            {
            }
        }
    }
}