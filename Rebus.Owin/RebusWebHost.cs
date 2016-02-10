using System;
using Microsoft.Owin.Hosting;
using Owin;
using Rebus.Bus;
using Rebus.Logging;

namespace Rebus.Owin
{
    class RebusWebHost: IInitializable, IDisposable
    {
        readonly Action<IAppBuilder> _startup;
        readonly string _listenUrl;
        readonly ILog _logger;

        IDisposable _host;

        public RebusWebHost(IRebusLoggerFactory rebusLoggerFactory, string listenUrl, Action<IAppBuilder> startup)
        {
            if (rebusLoggerFactory == null) throw new ArgumentNullException(nameof(rebusLoggerFactory));
            if (listenUrl == null) throw new ArgumentNullException(nameof(listenUrl));
            if (startup == null) throw new ArgumentNullException(nameof(startup));
            _listenUrl = listenUrl;
            _startup = startup;
            _logger = rebusLoggerFactory.GetCurrentClassLogger();
        }

        public void Initialize()
        {
            _logger.Info($"Starting web host listening on {_listenUrl}");
            _host = WebApp.Start(_listenUrl, _startup);
        }

        public void Dispose()
        {
            _logger.Info($"Stopping web host listening on {_listenUrl}");
            _host?.Dispose();
            _host = null;
        }
    }
}