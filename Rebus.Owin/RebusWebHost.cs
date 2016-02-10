using System;
using System.Collections.Generic;
using System.Linq;
using Owin;
using Rebus.Bus;
using Rebus.Logging;

namespace Rebus.Owin
{
    class RebusWebHost: IInitializable, IDisposable
    {
        readonly List<Endpoint> _endpoints = new List<Endpoint>();
        readonly ILog _logger;

        public RebusWebHost(IRebusLoggerFactory rebusLoggerFactory)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));

            _logger = rebusLoggerFactory.GetCurrentClassLogger();
        }

        public void AddEndpoint(string listenUrl, Action<IAppBuilder> startup)
        {
            _endpoints.Add(new Endpoint(_logger, listenUrl, startup));
        }

        public void Initialize()
        {
            foreach (var endpoint in _endpoints)
            {
                endpoint.Start();
            }
        }

        public void Dispose()
        {
            foreach (var endpoint in Enumerable.Reverse(_endpoints))
            {
                endpoint.Dispose();
            }
        }
    }
}