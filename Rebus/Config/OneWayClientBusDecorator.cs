using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.DataBus;
using Rebus.Logging;

namespace Rebus.Config;

class OneWayClientBusDecorator : IBus
{
    readonly IBus _innerBus;
    readonly AdvancedApiDecorator _advancedApiDecorator;

    public OneWayClientBusDecorator(IBus innerBus, IRebusLoggerFactory rebusLoggerFactory)
    {
        _innerBus = innerBus;
        _advancedApiDecorator = new AdvancedApiDecorator(_innerBus.Advanced, rebusLoggerFactory);
    }

    public void Dispose()
    {
        _innerBus.Dispose();
    }

    public Task SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null)
    {
        return _innerBus.SendLocal(commandMessage, optionalHeaders);
    }

    public Task Send(object commandMessage, IDictionary<string, string> optionalHeaders = null)
    {
        return _innerBus.Send(commandMessage, optionalHeaders);
    }

    public Task Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null)
    {
        return _innerBus.Reply(replyMessage, optionalHeaders);
    }

    public Task Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
    {
        return _innerBus.Defer(delay, message, optionalHeaders);
    }

    public Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
    {
        return _innerBus.DeferLocal(delay, message, optionalHeaders);
    }

    public IAdvancedApi Advanced => _advancedApiDecorator;

    public Task Subscribe<TEvent>()
    {
        return _innerBus.Subscribe<TEvent>();
    }

    public Task Subscribe(Type eventType)
    {
        return _innerBus.Subscribe(eventType);
    }

    public Task Unsubscribe<TEvent>()
    {
        return _innerBus.Unsubscribe<TEvent>();
    }

    public Task Unsubscribe(Type eventType)
    {
        return _innerBus.Unsubscribe(eventType);
    }

    public Task Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null)
    {
        return _innerBus.Publish(eventMessage, optionalHeaders);
    }

    class AdvancedApiDecorator : IAdvancedApi
    {
        readonly IAdvancedApi _innerAdvancedApi;
        readonly IRebusLoggerFactory _rebusLoggerFactory;

        public AdvancedApiDecorator(IAdvancedApi innerAdvancedApi, IRebusLoggerFactory rebusLoggerFactory)
        {
            _innerAdvancedApi = innerAdvancedApi;
            _rebusLoggerFactory = rebusLoggerFactory;
        }

        public IWorkersApi Workers => new OneWayClientWorkersApi(_rebusLoggerFactory);

        public ITopicsApi Topics => _innerAdvancedApi.Topics;

        public IRoutingApi Routing => _innerAdvancedApi.Routing;

        public ITransportMessageApi TransportMessage => _innerAdvancedApi.TransportMessage;

        public IDataBus DataBus => _innerAdvancedApi.DataBus;

        public ISyncBus SyncBus => _innerAdvancedApi.SyncBus;
    }

    class OneWayClientWorkersApi : IWorkersApi
    {
        readonly ILog _log;

        public OneWayClientWorkersApi(IRebusLoggerFactory rebusLoggerFactory)
        {
            _log = rebusLoggerFactory.GetLogger<OneWayClientWorkersApi>();
        }

        public int Count => 0;

        public void SetNumberOfWorkers(int numberOfWorkers)
        {
            if (numberOfWorkers <= 0) return;

            _log.Warn("Attempted to set number of workers to {numberOfWorkers}, but this is a one-way client!", numberOfWorkers);
        }
    }
}